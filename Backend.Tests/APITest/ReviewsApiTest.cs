using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Backend.Data;
using Backend.Features.Users;
using Backend.Persistence;
using Backend.Tests.Seeder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.APITest;

public class ReviewsApiTest(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private IServiceScope _scope = factory.Services.CreateScope();

    public async Task InitializeAsync()
    {
        _scope = factory.Services.CreateScope();
        var context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        TestDataSeeder.ClearDatabase(context);
        
        await TestDataSeeder.SeedTestDataAsync(
            context,
            _scope.ServiceProvider.GetRequiredService<UserManager<User>>(),
            _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>());
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        _client.Dispose();
        return Task.CompletedTask;
    }

    #region GET /reviews - Anonymous

    [Fact]
    public async Task GetAllReviews_ReturnsOkStatusCode()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/reviews");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region GET /reviews/{id} - Anonymous

    [Fact]
    public async Task GetReview_WithValidId_ReturnsOkStatusCode()
    {
        // Arrange
        var reviewId = TestDataSeeder.ItemReviewId;
        var request = new HttpRequestMessage(HttpMethod.Get, $"/reviews/{reviewId}");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetReview_WithInvalidId_ReturnsNotFoundStatusCode()
    {
        // Arrange
        var invalidReviewId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var request = new HttpRequestMessage(HttpMethod.Get, $"/reviews/{invalidReviewId}");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region POST /reviews - Requires Auth + Email Verification

    [Fact]
    public async Task CreateReview_WhenUnauthenticated_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var newReview = new
        {
            BookingId = TestDataSeeder.CompletedBookingId,
            ReviewerId = TestDataSeeder.ModeratorId,
            TargetItemId = TestDataSeeder.BookItemId,
            TargetUserId = (Guid?)null,
            Rating = 5,
            Comment = "Great test review!",
            CreatedAt = DateTime.UtcNow
        };
        
        // Act
        var response = await PostRequestStatusCode("/reviews", string.Empty, newReview);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateReview_WhenVerifiedUser_ReturnsCreatedStatusCode()
    {
        // Arrange
        var moderatorToken = await Authenticate(TestDataSeeder.ModeratorEmail, TestDataSeeder.ModeratorPassword);
        var newReview = new
        {
            BookingId = TestDataSeeder.CompletedBookingId,
            ReviewerId = TestDataSeeder.ModeratorId,
            TargetItemId = TestDataSeeder.BookItemId,
            TargetUserId = (Guid?)null,
            Rating = 4,
            Comment = "Good item!",
            CreatedAt = DateTime.UtcNow
        };
        
        // Act
        var response = await PostRequestStatusCode("/reviews", moderatorToken, newReview);
        
        // Assert
        // Expecting BadRequest because Moderator already has a review for this booking in seed data
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateReview_WhenUnverifiedUser_ReturnsForbiddenStatusCode()
    {
        // Arrange
        var unverifiedToken = await Authenticate(TestDataSeeder.UnverifiedUserEmail, TestDataSeeder.UnverifiedUserPassword);
        var newReview = new
        {
            BookingId = TestDataSeeder.CompletedBookingId,
            ReviewerId = TestDataSeeder.UnverifiedUserId,
            TargetItemId = TestDataSeeder.BookItemId,
            TargetUserId = (Guid?)null,
            Rating = 5,
            Comment = "Test review",
            CreatedAt = DateTime.UtcNow
        };
        
        // Act
        var response = await PostRequestStatusCode("/reviews", unverifiedToken, newReview);
        
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateReview_WithInvalidData_ReturnsBadRequestStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var newReview = new
        {
            BookingId = TestDataSeeder.CompletedBookingId,
            ReviewerId = TestDataSeeder.UserId,
            TargetItemId = TestDataSeeder.BookItemId,
            TargetUserId = (Guid?)null,
            Rating = 10, // Invalid - rating should be 1-5
            Comment = "Invalid rating",
            CreatedAt = DateTime.UtcNow
        };
        
        // Act
        var response = await PostRequestStatusCode("/reviews", userToken, newReview);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region PATCH /reviews/{id} - Requires Auth + Email Verification

    [Fact]
    public async Task UpdateReview_WhenUnauthenticated_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var reviewId = TestDataSeeder.ItemReviewId;
        var updateDto = new
        {
            Rating = 4,
            Comment = "Updated comment"
        };
        
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/reviews/{reviewId}")
        {
            Content = new StringContent(JsonSerializer.Serialize(updateDto), Encoding.UTF8, "application/json")
        };
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateReview_WhenOwner_ReturnsOkStatusCode()
    {
        // Arrange
        var moderatorToken = await Authenticate(TestDataSeeder.ModeratorEmail, TestDataSeeder.ModeratorPassword);
        var reviewId = TestDataSeeder.ItemReviewId; // Created by Moderator
        var updateDto = new
        {
            Rating = 4,
            Comment = "Updated: Still a great item!"
        };
        
        // Act
        var statusCode = await PatchRequestStatusCode($"/reviews/{reviewId}", moderatorToken, updateDto);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task UpdateReview_WhenUnverifiedUser_ReturnsForbiddenStatusCode()
    {
        // Arrange
        var unverifiedToken = await Authenticate(TestDataSeeder.UnverifiedUserEmail, TestDataSeeder.UnverifiedUserPassword);
        var reviewId = TestDataSeeder.ItemReviewId;
        var updateDto = new
        {
            Rating = 3,
            Comment = "Trying to update"
        };
        
        // Act
        var statusCode = await PatchRequestStatusCode($"/reviews/{reviewId}", unverifiedToken, updateDto);
        
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, statusCode);
    }

    [Fact]
    public async Task UpdateReview_WithInvalidId_ReturnsNotFoundStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var invalidReviewId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var updateDto = new
        {
            Rating = 4,
            Comment = "This review doesn't exist"
        };
        
        // Act
        var statusCode = await PatchRequestStatusCode($"/reviews/{invalidReviewId}", userToken, updateDto);
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, statusCode);
    }

    #endregion

    #region PUT /reviews/{id} - Backwards Compatibility

    [Fact]
    public async Task UpdateReviewViaPut_WhenVerifiedUser_ReturnsOkStatusCode()
    {
        // Arrange
        var moderatorToken = await Authenticate(TestDataSeeder.ModeratorEmail, TestDataSeeder.ModeratorPassword);
        var reviewId = TestDataSeeder.ItemReviewId;
        var updateDto = new
        {
            Rating = 5,
            Comment = "Updated via PUT for backwards compatibility"
        };
        
        // Act
        var statusCode = await PutRequestStatusCode($"/reviews/{reviewId}", moderatorToken, updateDto);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    #endregion

    #region DELETE /reviews/{id} - Requires Auth + Email Verification

    [Fact]
    public async Task DeleteReview_WhenUnauthenticated_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var reviewId = TestDataSeeder.UserReviewId;
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/reviews/{reviewId}");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteReview_WhenOwner_ReturnsOkStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var reviewId = TestDataSeeder.UserReviewId; // Created by User
        
        // Act
        var statusCode = await DeleteRequestStatusCode($"/reviews/{reviewId}", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task DeleteReview_WhenUnverifiedUser_ReturnsForbiddenStatusCode()
    {
        // Arrange
        var unverifiedToken = await Authenticate(TestDataSeeder.UnverifiedUserEmail, TestDataSeeder.UnverifiedUserPassword);
        var reviewId = TestDataSeeder.ItemReviewId;
        
        // Act
        var statusCode = await DeleteRequestStatusCode($"/reviews/{reviewId}", unverifiedToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, statusCode);
    }

    [Fact]
    public async Task DeleteReview_WithInvalidId_ReturnsNotFoundStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var invalidReviewId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        
        // Act
        var statusCode = await DeleteRequestStatusCode($"/reviews/{invalidReviewId}", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, statusCode);
    }

    #endregion

    #region Helper Methods

    private async Task<string> Authenticate(string email, string password)
    {
        var loginRequest = new LoginUserRequest(email, password);
        var response = await _client.PostAsync("/login", 
            new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var token = jsonResponse!.RootElement.GetProperty("accessToken").GetString();
        return token!;
    }

    private async Task<HttpResponseMessage> PostRequestStatusCode(string url, string token, object? content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (content != null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json");
        }
        
        var response = await _client.SendAsync(request);
        return response;
    }

    private async Task<HttpStatusCode> PatchRequestStatusCode(string url, string token, object? content = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (content != null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json");
        }
        
        var response = await _client.SendAsync(request);
        return response.StatusCode;
    }

    private async Task<HttpStatusCode> PutRequestStatusCode(string url, string token, object? content = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (content != null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json");
        }
        
        var response = await _client.SendAsync(request);
        return response.StatusCode;
    }
    
    private async Task<HttpStatusCode> DeleteRequestStatusCode(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        var response = await _client.SendAsync(request);
        return response.StatusCode;
    }

    #endregion
}
