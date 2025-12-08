using System.Net;

namespace Backend.Tests.APITest;

public class ReviewsApiTest(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    
    [Fact]
    public async Task GetAllReviws_ReturnsOKStatusCode()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/reviews");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}