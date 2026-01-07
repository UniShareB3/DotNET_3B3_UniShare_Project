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

public class UniversitiesApiTest(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
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

    #region GET /universities - Anonymous

    [Fact]
    public async Task GetAllUniversities_ReturnsOkStatusCode()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/universities");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllUniversities_ReturnsListOfUniversities()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/universities");
        
        // Act
        var response = await _client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("UAIC", content);
        Assert.Contains("TUIASI", content);
        Assert.Contains("UPB", content);
    }

    #endregion

    #region POST /universities - Admin Only

    [Fact]
    public async Task CreateUniversity_WhenUnauthenticated_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var newUniversity = new
        {
            PostUniversityDto = new
            {
                Name = "New Test University",
                ShortCode = "NTU",
                EmailDomain = "@ntu.ro"
            }
        };
        
        // Act
        var response = await PostRequestStatusCode("/universities", string.Empty, newUniversity);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateUniversity_WhenUser_ReturnsForbiddenStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var newUniversity = new
        {
            PostUniversityDto = new
            {
                Name = "User Test University",
                ShortCode = "UTU",
                EmailDomain = "@utu.ro"
            }
        };
        
        // Act
        var response = await PostRequestStatusCode("/universities", userToken, newUniversity);
        
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateUniversity_WhenAdmin_ReturnsCreatedStatusCode()
    {
        // Arrange
        var adminToken = await Authenticate(TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);
        var newUniversity = new
        {
            PostUniversityDto = new
            {
                Name = "Admin Test University",
                ShortCode = "ATU",
                EmailDomain = "@atu.ro"
            }
        };
        
        // Act
        var response = await PostRequestStatusCode("/universities", adminToken, newUniversity);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
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

    #endregion
}
