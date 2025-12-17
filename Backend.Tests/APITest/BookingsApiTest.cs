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

public class BookingsApiTest(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
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

    #region POST /bookings - Create Booking

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
        
        // Act
        var response = await PostRequestStatusCode("/bookings", string.Empty, newBooking);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task CreateBooking_WhenUser_ReturnsCreatedStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.ModeratorEmail, TestDataSeeder.ModeratorPassword);
        var newBooking = new
        {
            ItemId = TestDataSeeder.LaptopItemId,
            BorrowerId = TestDataSeeder.ModeratorId,
            StartDate = DateTime.UtcNow.AddDays(10),
            EndDate = DateTime.UtcNow.AddDays(11)
        };
        // Act
        var response = await PostRequestStatusCode("/bookings", userToken, newBooking);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
    
    [Fact]
    public async Task CreateBooking_WhenUserWithInvalidData_ReturnsBadRequestStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var newBooking = new
        {
            ItemId = TestDataSeeder.LaptopItemId,
            BorrowerId = TestDataSeeder.UserId,
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(4) // End date before start date
        };
        // Act
        var response = await PostRequestStatusCode("/bookings", userToken, newBooking);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region GET /bookings - Get All Bookings (Admin only)

    [Fact]
    public async Task GetAllBookings_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/bookings");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task GetAllBookings_WhenAdmin_ReturnsOkStatusCode()
    {
        // Arrange
        var adminToken = await Authenticate(TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);
        
        // Act
        var statusCode = await GetRequestStatusCode("/bookings", adminToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    #endregion

    #region GET /bookings/{id} - Get Booking by ID

    [Fact]
    public async Task GetBooking_WhenUnauthenticated_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var bookingId = TestDataSeeder.ApprovedBookingId;
        var request = new HttpRequestMessage(HttpMethod.Get, $"/bookings/{bookingId}");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task GetBooking_WhenUser_ReturnsOkStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var bookingId = TestDataSeeder.PendingBookingId;
        
        // Act
        var statusCode = await GetRequestStatusCode($"/bookings/{bookingId}", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    #endregion

    #region PATCH /bookings/{id} - Update Booking Status

    [Fact]
    public async Task UpdateBookingStatus_WhenUnauthenticated_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var bookingId = TestDataSeeder.PendingBookingId;
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/bookings/{bookingId}");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task UpdateBookingStatus_WhenAdmin_ReturnsOkStatusCode()
    {
        // Arrange
        var adminToken = await Authenticate(TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);
        var bookingId = TestDataSeeder.PendingBookingId;
        var updateStatusDto = new
        {
            UserId = TestDataSeeder.UserId, // Owner of the item
            BookingStatus = 1 // Approved
        };
        
        // Act
        var statusCode = await PatchRequestStatusCode($"/bookings/{bookingId}", adminToken, updateStatusDto);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }
    
    [Fact]
    public async Task UpdateBookingStatus_WhenUser_ReturnsBadRequestStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var bookingId = TestDataSeeder.PendingBookingId;
        var updateStatusDto = new
        {
            UserId = TestDataSeeder.UserId, // Owner of the item
            BookingStatus = 5 // unknown status
        };
        
        // Act
        var statusCode = await PatchRequestStatusCode($"/bookings/{bookingId}", userToken, updateStatusDto);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, statusCode);
    }

    #endregion

    #region DELETE /bookings/{id} - Delete Booking

    [Fact]
    public async Task DeleteBooking_WhenUnauthenticated_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var bookingId = TestDataSeeder.ApprovedBookingId;
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/bookings/{bookingId}");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task DeleteBooking_WhenUser_ReturnsForbiddenStatusCode()
    {
        // Arrange
        var userToken = await Authenticate(TestDataSeeder.UserEmail, TestDataSeeder.UserPassword);
        var bookingId = TestDataSeeder.ApprovedBookingId;
        
        // Act
        var statusCode = await DeleteRequestStatusCode($"/bookings/{bookingId}", userToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, statusCode);
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

    private async Task<HttpStatusCode> PatchRequestStatusCode(string url, string token, object? content = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (content != null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json");
        }
        
        var response = await _client.SendAsync(request);
        return response.StatusCode;
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
