namespace Backend.Tests.APITest;

public class UserApiTest(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient();
    
    [Fact]
    public async Task GetAllUsers_ReturnsSuccessStatusCode()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/users");
        
        // Act
        var response = await client.SendAsync(request);
        
        // Assert
        response.EnsureSuccessStatusCode();
    }
    
}