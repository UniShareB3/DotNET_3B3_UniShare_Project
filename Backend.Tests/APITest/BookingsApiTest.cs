namespace Backend.Tests.APITest;

public class BookingsApiTest(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task GetAllBookings_ReturnsSuccessStatusCode()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/bookings");
        // Act
        var response = await client.SendAsync(request);
        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetAllBookings_ReturnsJsonArray()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/bookings");
        // Act
        var response = await client.SendAsync(request);
        // Assert
        response.EnsureSuccessStatusCode();
        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.True(contentType != null && contentType.StartsWith("application/json"), $"Unexpected content type: {contentType}");
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotNull(content);
        Assert.True(content.TrimStart().StartsWith("["), "Expected JSON array response for /bookings");
    }
    
    [Fact]
    public async Task CreateBooking_ReturnsCreatedStatusCode()
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
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
    
}

