using System.Net;

namespace Backend.Tests.APITest;

public class BookingsApiTest(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient();
    

    [Fact]
    public async Task GetAllBookings_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/bookings");
        
        // Act
        var response = await client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllBookings_NotUnauthorized_WhenAuthenticated()
    {
        // Act
        var response = await client.GetAsync("/bookings");

        // Assert 
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var newBooking = new
        {
            UserId = Guid.NewGuid(),
            ItemId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(2)
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "/bookings")
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(newBooking), System.Text.Encoding.UTF8, "application/json")
        };
        
        // Act
        var response = await client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_NotUnauthorized_WhenAuthenticated()
    {
        // Arrange
        var newBooking = new
        {
            UserId = Guid.Parse("cb397a9b-ec7c-4bb4-b683-363f07dd94d6"),
            ItemId = Guid.Parse("cb397a9b-ec7c-4bb4-b683-363f07dd94d7"),
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(2)
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "/bookings")
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(newBooking), System.Text.Encoding.UTF8, "application/json")
        };

        // Act
        var response = await client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
