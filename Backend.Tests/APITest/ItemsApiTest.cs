namespace Backend.Tests.APITest;

public class ItemsApiTest: IClassFixture<CustomWebApplicationFactory>
{
    private CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    
    private readonly string _webUrl = "https://localhost:7112";

    public ItemsApiTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        
        _factory.HostUrl = _webUrl;
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

    [Fact]
    public async Task GetAllItems_ReturnsJsonArray()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/items");
        // Act
        var response = await _client.SendAsync(request);
        // Assert
        response.EnsureSuccessStatusCode();
        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.True(contentType != null && contentType.StartsWith("application/json"), $"Unexpected content type: {contentType}");
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotNull(content);

        Assert.True(content.TrimStart().StartsWith("["), "Expected JSON array response for /items");
    }
}
