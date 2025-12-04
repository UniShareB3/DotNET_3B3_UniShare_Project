using System.Net;

namespace Backend.Tests.APITest;

public class UserApiTest(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    
    [Fact]
    public async Task GetAllUsers_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/users");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
}