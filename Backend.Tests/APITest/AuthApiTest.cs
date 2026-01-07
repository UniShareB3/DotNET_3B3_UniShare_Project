using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Backend.Data;
using Backend.Features.Users.LoginUser;
using Backend.Persistence;
using Backend.Tests.Seeder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.APITest;

public class AuthApiTest(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
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

    #region POST /login - Anonymous

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var loginDto = new
        {
            Email = TestDataSeeder.UserEmail,
            Password = TestDataSeeder.UserPassword
        };

        // Act
        var response = await _client.PostAsync("/login",
            new StringContent(JsonSerializer.Serialize(loginDto), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("accessToken", content);
        Assert.Contains("refreshToken", content);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new
        {
            Email = TestDataSeeder.UserEmail,
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsync("/login",
            new StringContent(JsonSerializer.Serialize(loginDto), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new
        {
            Email = "nonexistent@test.com",
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsync("/login",
            new StringContent(JsonSerializer.Serialize(loginDto), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region POST /register - Anonymous

    [Fact]
    public async Task PostRegister_WithValidData_ReturnsCreated()
    {
        // Arrange
        var registerDto = new
        {
            Email = "newuser@student.uaic.ro",
            Password = "NewUser123!",
            FirstName = "New",
            LastName = "User",
            UniversityName = "Universitatea Alexandru Ioan Cuza"
        };

        // Act
        var response = await _client.PostAsync("/register",
            new StringContent(JsonSerializer.Serialize(registerDto), Encoding.UTF8, "application/json"));

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.Created
        );
    }

    [Fact]
    public async Task PostRegister_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new
        {
            Email = TestDataSeeder.UserEmail,
            Password = "Password123!",
            FirstName = "Duplicate",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsync("/register",
            new StringContent(JsonSerializer.Serialize(registerDto), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region POST /refresh - Anonymous

    [Fact]
    public async Task PostRefreshToken_WithValidToken_ReturnsOkWithNewToken()
    {
        // Arrange 
        var loginDto = new
        {
            Email = TestDataSeeder.UserEmail,
            Password = TestDataSeeder.UserPassword
        };
        var loginResponse = await _client.PostAsync("/login",
            new StringContent(JsonSerializer.Serialize(loginDto), Encoding.UTF8, "application/json"));
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var refreshToken = loginContent!.RootElement.GetProperty("refreshToken").GetString();

        var refreshDto = new { RefreshToken = refreshToken };

        // Act
        var response = await _client.PostAsync("/refresh",
            new StringContent(JsonSerializer.Serialize(refreshDto), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("accessToken", content);
    }

    [Fact]
    public async Task PostRefreshToken_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshDto = new { RefreshToken = "invalid-refresh-token" };

        // Act
        var response = await _client.PostAsync("/refresh",
            new StringContent(JsonSerializer.Serialize(refreshDto), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region POST /auth/email-confirmation - Anonymous

    [Fact]
    public async Task PostConfirmEmail_WithInvalidCode_ReturnsBadRequest()
    {
        // Arrange
        var confirmDto = new
        {
            UserId = TestDataSeeder.UnverifiedUserId,
            Code = "INVALID"
        };

        // Act
        var response = await _client.PostAsync("/auth/email-confirmation",
            new StringContent(JsonSerializer.Serialize(confirmDto), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region POST /auth/verification-code - Owner or Admin

    [Fact]
    public async Task PostSendVerificationCode_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var verificationDto = new
        {
            UserId = TestDataSeeder.UnverifiedUserId
        };

        // Act
        var response = await _client.PostAsync("/auth/verification-code",
            new StringContent(JsonSerializer.Serialize(verificationDto), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostSendVerificationCode_WhenDifferentUser_ReturnsForbidden()
    {
        // Arrange 
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var verificationDto = new
        {
            UserId = TestDataSeeder.UnverifiedUserId
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/verification-code")
        {
            Content = new StringContent(JsonSerializer.Serialize(verificationDto), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region POST /auth/password-reset/request - Anonymous

    [Fact]
    public async Task PostRequestPasswordReset_WithNonExistentEmail_ReturnsNotFound()
    {
        // Arrange
        var resetDto = new
        {
            Email = "nonexistent@test.com"
        };

        // Act
        var response = await _client.PostAsync("/auth/password-reset/request",
            new StringContent(JsonSerializer.Serialize(resetDto), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region GET /auth/password - Anonymous

    [Fact]
    public async Task GetVerifyPasswordReset_WithInvalidCode_ReturnsBadRequest()
    {
        // Arrange
        var userId = TestDataSeeder.UserId;
        var invalidCode = "INVALIDCODE";

        // Act
        var response = await _client.GetAsync($"/auth/password?userId={userId}&code={invalidCode}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    #endregion
}