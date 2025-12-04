using Backend.Data;
using Backend.Features.Bookings;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Handlers.Bookings;

public class GetAllBookingsHandlerTests
{
    private static ApplicationContext CreateInMemoryDbContext(string guid)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: guid)
            .Options;

        var context = new ApplicationContext(options);
        return context;
    }
 
    [Fact]
    public async Task Given_BookingsExist_When_Handle_Then_ReturnsAllBookings()
    {
        // Arrange
        var context = CreateInMemoryDbContext("get-all-bookings-test-db");
        
        var booking1 = new Booking
        {
            Id = Guid.NewGuid(),
            ItemId = Guid.NewGuid(),
            BorrowerId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(2)
        };
        
        var booking2 = new Booking
        {
            Id = Guid.NewGuid(),
            ItemId = Guid.NewGuid(),
            BorrowerId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow.AddDays(3),
            EndDate = DateTime.UtcNow.AddDays(5)
        };
        
        context.Bookings.AddRange(booking1, booking2);
        await context.SaveChangesAsync();
        
        var handler = new GetAllBookingsHandler(context);
        var request = new GetAllBookingsRequest();
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        var bookings = valueResult.Value.Should().BeAssignableTo<List<Booking>>().Subject;
        
        bookings.Should().HaveCount(2);
        bookings.Should().Contain(b => b.Id == booking1.Id);
        bookings.Should().Contain(b => b.Id == booking2.Id);
    }
    
    [Fact]
    public async Task Given_NoBookingsExist_When_Handle_Then_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateInMemoryDbContext("get-all-bookings-empty-test-db");
        
        var handler = new GetAllBookingsHandler(context);
        var request = new GetAllBookingsRequest();
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        var bookings = valueResult.Value.Should().BeAssignableTo<List<Booking>>().Subject;
        
        bookings.Should().BeEmpty();
    }
}