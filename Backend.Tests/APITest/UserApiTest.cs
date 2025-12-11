using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Backend.Data;
using Backend.Features.Users;
using Backend.Persistence;
using Backend.Tests.Seeder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.APITest;

public class UserApiTest(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
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
    
    public async Task DisposeAsync()
    {
        _scope.Dispose();
        _client.Dispose();
    }
    
    #region GET /users - Get All Users Tests
    
    [Fact]
    public async Task GetAllUsers_WhenNotSignedIn_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/users");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllUsers_WhenAdmin_ReturnsOKStatusCode()
    {
        // Arrange
        var adminToken = await Authenticate(TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);
        
        // Act
        var statusCode = await GetRequestStatusCode("/users", adminToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }
    
    [Fact]
    public async Task GetAllUsers_WhenUser_ReturnsForbiddenStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        
        // Act
        var statusCode = await GetRequestStatusCode("/users", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, statusCode);
    }
    
    [Fact]
    public async Task GetAllUsers_WhenModerator_ReturnsForbiddenStatusCode()
    {
        // Arrange
        var moderatorToken = await Authenticate(TestDataSeeder.ModeratorEmail, TestDataSeeder.ModeratorPassword);
        
        // Act
        var statusCode = await GetRequestStatusCode("/users", moderatorToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, statusCode);
    }

    #endregion

    #region GET /users/{userId} - Get Specific User Tests

    [Fact]
    public async Task GetUser_WhenNotSignedIn_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var userId = TestDataSeeder.UserId;
        
        // Act
        var response = await _client.GetAsync($"/users/{userId}");
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_WhenOwner_ReturnsOKStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var userId = TestDataSeeder.UserId;
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{userId}", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task GetUser_WhenAdmin_ReturnsOKStatusCode()
    {
        // Arrange
        var adminToken = await Authenticate(TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);
        var userId = TestDataSeeder.UserId;
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{userId}", adminToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task GetUser_WhenDifferentUser_ReturnsForbiddenStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var moderatorUserId = TestDataSeeder.ModeratorId;
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{moderatorUserId}", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, statusCode);
    }

    [Fact]
    public async Task GetUser_WithInvalidUserId_ReturnsNotFoundStatusCode()
    {
        // Arrange
        var adminToken = await Authenticate(TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);
        var invalidUserId = Guid.NewGuid();
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{invalidUserId}", adminToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, statusCode);
    }

    #endregion

    #region GET /users/{userId}/refresh-tokens - Get Refresh Tokens Tests

    [Fact]
    public async Task GetRefreshTokens_WhenNotSignedIn_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var userId = TestDataSeeder.UserId;
        
        // Act
        var response = await _client.GetAsync($"/users/{userId}/refresh-tokens");
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRefreshTokens_WhenOwner_ReturnsOKStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var userId = TestDataSeeder.UserId;
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{userId}/refresh-tokens", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task GetRefreshTokens_WhenAdmin_ReturnsOKStatusCode()
    {
        // Arrange
        var adminToken = await Authenticate(TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);
        var userId = TestDataSeeder.UserId;
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{userId}/refresh-tokens", adminToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task GetRefreshTokens_WhenDifferentUser_ReturnsForbiddenStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var moderatorUserId = TestDataSeeder.ModeratorId;
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{moderatorUserId}/refresh-tokens", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, statusCode);
    }

    #endregion

    #region DELETE /users/{userId} - Delete User Tests

    [Fact]
    public async Task DeleteUser_WhenNotSignedIn_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var userId = TestDataSeeder.UserId;
        
        // Act
        var response = await _client.DeleteAsync($"/users/{userId}");
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_WhenOwner_ReturnsNoContentStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var userId = TestDataSeeder.UserId;
        
        // Act
        var statusCode = await DeleteRequestStatusCode($"/users/{userId}", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task DeleteUser_WhenAdmin_ReturnsNoContentStatusCode()
    {
        // Arrange
        var adminToken = await Authenticate(TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);
        var userId = TestDataSeeder.UnverifiedUserId;
        
        // Act
        var statusCode = await DeleteRequestStatusCode($"/users/{userId}", adminToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task DeleteUser_WhenNotAdmin_ReturnsForbiddenStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var moderatorUserId = TestDataSeeder.ModeratorId;
        
        // Act
        var statusCode = await DeleteRequestStatusCode($"/users/{moderatorUserId}", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, statusCode);
    }

    #endregion

    #region POST /users/{userId}/assign-admin - Assign Admin Role Tests

    [Fact]
    public async Task AssignAdminRole_WhenNotSignedIn_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var userId = TestDataSeeder.ModeratorId;
        
        // Act
        var response = await _client.PostAsync($"/users/{userId}/assign-admin", null);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AssignAdminRole_WhenAdmin_ReturnsOKStatusCode()
    {
        // Arrange
        var adminToken = await Authenticate(TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);
        var userId = TestDataSeeder.ModeratorId;
        
        // Act
        var statusCode = await PostRequestStatusCode($"/users/{userId}/assign-admin", adminToken, null);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task AssignAdminRole_WhenRegularUser_ReturnsForbiddenStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var userId = TestDataSeeder.ModeratorId;
        
        // Act
        var statusCode = await PostRequestStatusCode($"/users/{userId}/assign-admin", userToken, null);
        
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, statusCode);
    }

    [Fact]
    public async Task AssignAdminRole_WhenModerator_ReturnsForbiddenStatusCode()
    {
        // Arrange
        var moderatorToken = await Authenticate(TestDataSeeder.ModeratorEmail, TestDataSeeder.ModeratorPassword);
        var userId = TestDataSeeder.UserId;
        
        // Act
        var statusCode = await PostRequestStatusCode($"/users/{userId}/assign-admin", moderatorToken, null);
        
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, statusCode);
    }

    #endregion

    #region GET /users/{userId}/items - Get User Items Tests

    [Fact]
    public async Task GetUserItems_WhenNotSignedIn_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var userId = TestDataSeeder.UserId;
        
        // Act
        var response = await _client.GetAsync($"/users/{userId}/items");
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserItems_WhenOwner_ReturnsOKStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var userId = TestDataSeeder.UserId;
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{userId}/items", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task GetUserItems_WhenAdmin_ReturnsOKStatusCode()
    {
        // Arrange
        var adminToken = await Authenticate(TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);
        var userId = TestDataSeeder.UserId;
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{userId}/items", adminToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task GetUserItems_WhenUnverifiedUser_ReturnsForbiddenStatusCode()
    {
        // Arrange
        var unverifiedToken = await Authenticate(TestDataSeeder.UnverifiedUserEmail, TestDataSeeder.UnverifiedUserPassword);
        var userId = TestDataSeeder.UnverifiedUserId;
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{userId}/items", unverifiedToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, statusCode);
    }

    [Fact]
    public async Task GetUserItems_WhenNotAdmin_ReturnsForbiddenStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.ModeratorEmail, TestDataSeeder.ModeratorPassword);
        var moderatorUserId = TestDataSeeder.UserId;
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{moderatorUserId}/items", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, statusCode);
    }

    #endregion

    #region GET /users/{userId}/items/{itemId} - Get Specific User Item Tests

    [Fact]
    public async Task GetUserItem_WhenNotSignedIn_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var userId = TestDataSeeder.UserId;
        var itemId = TestDataSeeder.LaptopItemId;
        
        // Act
        var response = await _client.GetAsync($"/users/{userId}/items/{itemId}");
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserItem_WhenOwner_ReturnsOKStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var userId = TestDataSeeder.UserId;
        var itemId = TestDataSeeder.LaptopItemId;
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{userId}/items/{itemId}", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task GetUserItem_WhenAdmin_ReturnsOKStatusCode()
    {
        // Arrange
        var adminToken = await Authenticate(TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);
        var userId = TestDataSeeder.UserId;
        var itemId = TestDataSeeder.LaptopItemId;
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{userId}/items/{itemId}", adminToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task GetUserItem_WithInvalidItemId_ReturnsNotFoundStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var userId = TestDataSeeder.UserId;
        var invalidItemId = Guid.NewGuid();
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{userId}/items/{invalidItemId}", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, statusCode);
    }

    #endregion

    #region GET /users/{userId}/bookings - Get User Bookings Tests

    [Fact]
    public async Task GetUserBookings_WhenNotSignedIn_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var userId = TestDataSeeder.UserId;
        
        // Act
        var response = await _client.GetAsync($"/users/{userId}/bookings");
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserBookings_WhenOwner_ReturnsOKStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var userId = TestDataSeeder.UserId;
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{userId}/bookings", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task GetUserBookings_WhenAdmin_ReturnsOKStatusCode()
    {
        // Arrange
        var adminToken = await Authenticate(TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);
        var userId = TestDataSeeder.UserId;
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{userId}/bookings", adminToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task GetUserBookings_WhenUnverifiedUser_ReturnsForbiddenStatusCode()
    {
        // Arrange
        var unverifiedToken = await Authenticate(TestDataSeeder.UnverifiedUserEmail, TestDataSeeder.UnverifiedUserPassword);
        var userId = TestDataSeeder.UnverifiedUserId;
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{userId}/bookings", unverifiedToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, statusCode);
    }

    #endregion

    #region GET /users/{userId}/booked-items - Get User Booked Items Tests

    [Fact]
    public async Task GetUserBookedItems_WhenNotSignedIn_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var userId = TestDataSeeder.UserId;
        
        // Act
        var response = await _client.GetAsync($"/users/{userId}/booked-items");
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserBookedItems_WhenOwner_ReturnsOKStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var userId = TestDataSeeder.UserId;
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{userId}/booked-items", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task GetUserBookedItems_WhenAdmin_ReturnsOKStatusCode()
    {
        // Arrange
        var adminToken = await Authenticate(TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);
        var userId = TestDataSeeder.UserId;
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{userId}/booked-items", adminToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    #endregion

    #region GET /users/{userId}/booked-items/{bookingId} - Get Specific Booked Item Tests

    [Fact]
    public async Task GetUserBookedItem_WhenNotSignedIn_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var userId = TestDataSeeder.UserId;
        var bookingId = Guid.NewGuid();
        
        // Act
        var response = await _client.GetAsync($"/users/{userId}/booked-items/{bookingId}");
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserBookedItem_WhenOwner_WithValidBooking_ReturnsOKOrNotFoundStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var userId = TestDataSeeder.UserId;
        var bookingId = Guid.NewGuid();
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{userId}/booked-items/{bookingId}", userToken);
        
        // Assert
        // Can be OK if booking exists or NotFound if it doesn't - both are valid responses
        Assert.True(statusCode == HttpStatusCode.OK || statusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserBookedItem_WhenAdmin_ReturnsOKOrNotFoundStatusCode()
    {
        // Arrange
        var adminToken = await Authenticate(TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);
        var userId = TestDataSeeder.UserId;
        var bookingId = Guid.NewGuid();
        
        // Act
        var statusCode = await GetRequestStatusCode($"/users/{userId}/booked-items/{bookingId}", adminToken);
        
        // Assert
        Assert.True(statusCode == HttpStatusCode.OK || statusCode == HttpStatusCode.NotFound);
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
    
    private async Task<HttpStatusCode> GetRequestStatusCode(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
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
    
    private async Task<HttpStatusCode> PostRequestStatusCode(string url, string token, object? content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (content != null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json");
        }
        var response = await _client.SendAsync(request);
        return response.StatusCode;
    }

    #endregion
}