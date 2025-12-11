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

public class ItemsApiTest(CustomWebApplicationFactory factory): IClassFixture<CustomWebApplicationFactory>
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

    [Fact]
    public async Task GetAllItems_ReturnsSuccessStatusCode()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/items");
        // Act
        var response = await _client.SendAsync(request);
        // Assert
        response.EnsureSuccessStatusCode();
    }

    
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
    
}
