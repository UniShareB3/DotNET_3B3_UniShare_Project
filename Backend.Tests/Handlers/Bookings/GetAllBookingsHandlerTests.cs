using Backend.Data;
using Backend.Features.Bookings;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

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
        var context = CreateInMemoryDbContext("a1b2c3d4-eaf6-7a8b-9c0d-e1f2a3b4c5d6");
        
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
        var context = CreateInMemoryDbContext("a1b2c3d4-e0f6-7a8b-9c0d-e1f2a3b4c5d6");
        
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
    
    [Fact]
    public async Task Given_ExceptionOccurs_When_Handle_Then_ReturnsProblemResult()
    {
        // Arrange
        var handler = new GetAllBookingsHandler(null!);
        var request = new GetAllBookingsRequest();
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        var problemResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        problemResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}