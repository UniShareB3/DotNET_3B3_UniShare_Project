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

public class ItemsApiTest(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
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

    #region GET /items - Anonymous

    [Fact]
    public async Task GetAllItems_ReturnsOkStatusCode()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/items");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region GET /items/{id} - Anonymous

    [Fact]
    public async Task GetItem_WithValidId_ReturnsOkStatusCode()
    {
        // Arrange
        var itemId = TestDataSeeder.LaptopItemId;
        var request = new HttpRequestMessage(HttpMethod.Get, $"/items/{itemId}");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetItem_WithInvalidId_ReturnsNotFoundStatusCode()
    {
        // Arrange
        var invalidItemId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var request = new HttpRequestMessage(HttpMethod.Get, $"/items/{invalidItemId}");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region POST /items - Requires Auth + Email Verification

    [Fact]
    public async Task CreateItem_WhenUnauthenticated_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var newItem = new
        {
            Item = new
            {
                Name = "New Test Item",
                Description = "A new test item",
                Category = "Electronics",
                Condition = "New",
                OwnerId = TestDataSeeder.UserId,
                ImageUrl = (string?)null
            }
        };
        
        // Act
        var response = await PostRequestStatusCode("/items", string.Empty, newItem);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateItem_WhenAuthenticatedUser_ReturnsCreatedStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var newItem = new
        {
            Item = new
            {
                Name = "New Test Item",
                Description = "A new test item",
                Category = "Electronics",
                Condition = "New",
                OwnerId = TestDataSeeder.UserId,
                ImageUrl = (string?)null
            }
        };
        
        // Act
        var response = await PostRequestStatusCode("/items", userToken, newItem);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateItem_WhenUnverifiedUser_ReturnsForbiddenStatusCode()
    {
        // Arrange
        var unverifiedToken = await Authenticate(TestDataSeeder.UnverifiedUserEmail, TestDataSeeder.UnverifiedUserPassword);
        var newItem = new
        {
            Item = new
            {
                Name = "New Test Item",
                Description = "A new test item",
                Category = "Electronics",
                Condition = "New",
                OwnerId = TestDataSeeder.UnverifiedUserId,
                ImageUrl = (string?)null
            }
        };
        
        // Act
        var response = await PostRequestStatusCode("/items", unverifiedToken, newItem);
        
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateItem_WhenAdmin_ReturnsCreatedStatusCode()
    {
        // Arrange
        var adminToken = await Authenticate(TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);
        var newItem = new
        {
            Item = new
            {
                Name = "Admin Test Item",
                Description = "An admin test item",
                Category = "Books",
                Condition = "New",
                OwnerId = TestDataSeeder.AdminId,
                ImageUrl = (string?)null
            }
        };
        
        // Act
        var response = await PostRequestStatusCode("/items", adminToken, newItem);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateItem_WithInvalidData_ReturnsBadRequestStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var newItem = new
        {
            Item = new
            {
                Name = "", // Invalid - empty name
                Description = "A test item",
                Category = "Electronics",
                Condition = "New",
                OwnerId = TestDataSeeder.UserId,
                ImageUrl = (string?)null
            }
        };
        
        // Act
        var response = await PostRequestStatusCode("/items", userToken, newItem);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region DELETE /items/{id} - Requires Auth + Email Verification

    [Fact]
    public async Task DeleteItem_WhenUnauthenticated_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var itemId = TestDataSeeder.LaptopItemId;
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/items/{itemId}");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteItem_WhenOwner_ReturnsOkStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var itemId = TestDataSeeder.BookItemId; // Owned by User
        
        // Act
        var statusCode = await DeleteRequestStatusCode($"/items/{itemId}", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task DeleteItem_WhenAdmin_ReturnsOkStatusCode()
    {
        // Arrange
        var adminToken = await Authenticate(TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);
        var itemId = TestDataSeeder.JacketItemId; // Owned by User, but Admin can delete
        
        // Act
        var statusCode = await DeleteRequestStatusCode($"/items/{itemId}", adminToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task DeleteItem_WhenUnverifiedUser_ReturnsForbiddenStatusCode()
    {
        // Arrange
        var unverifiedToken = await Authenticate(TestDataSeeder.UnverifiedUserEmail, TestDataSeeder.UnverifiedUserPassword);
        var itemId = TestDataSeeder.LaptopItemId;
        
        // Act
        var statusCode = await DeleteRequestStatusCode($"/items/{itemId}", unverifiedToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, statusCode);
    }

    [Fact]
    public async Task DeleteItem_WithInvalidId_ReturnsNotFoundStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var invalidItemId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        
        // Act
        var statusCode = await DeleteRequestStatusCode($"/items/{invalidItemId}", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, statusCode);
    }

    #endregion

    #region GET /items/{id}/bookings - Anonymous

    [Fact]
    public async Task GetBookingsForItem_WithValidId_ReturnsOkStatusCode()
    {
        // Arrange
        var itemId = TestDataSeeder.LaptopItemId;
        var request = new HttpRequestMessage(HttpMethod.Get, $"/items/{itemId}/bookings");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetBookingsForItem_WithInvalidId_ReturnsNotFoundStatusCode()
    {
        // Arrange
        var invalidItemId = Guid.Parse("abcdefab-cdef-abcd-efab-cdefabcdefab");
        var request = new HttpRequestMessage(HttpMethod.Get, $"/items/{invalidItemId}/bookings");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

    private async Task<HttpStatusCode> DeleteRequestStatusCode(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        var response = await _client.SendAsync(request);
        return response.StatusCode;
    }

    #endregion
}
