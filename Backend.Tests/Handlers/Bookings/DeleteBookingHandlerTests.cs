using Backend.Data;
using Backend.Features.Bookings.DeleteBooking;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Handlers.Bookings;

public class DeleteBookingHandlerTests
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
    public async Task Given_BookingExists_When_Handle_Then_RemovesBookingAndReturnsNoContent()
    {
        // Arrange
        var context = CreateInMemoryDbContext("12345678-1234-1234-1234-1234567890ab");
        var handler = new DeleteBookingHandler(context);
        var bookingId = Guid.Parse("12345678-1234-1234-1234-1234567890a5");
        var booking = new Booking { Id = bookingId, ItemId = Guid.Parse("12345678-1234-1234-1234-123456789077"), BorrowerId = Guid.Parse("12345678-1234-1234-1234-123456789078"), StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var request = new DeleteBookingRequest(bookingId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var removed = await context.Bookings.FindAsync(bookingId);
        Assert.Null(removed);
    }

    [Fact]
    public async Task Given_BookingDoesNotExist_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext("87654321-4321-4321-4321-ba0987654321");
        var handler = new DeleteBookingHandler(context);
        var request = new DeleteBookingRequest(Guid.NewGuid());

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
    
    [Fact]
    public async Task Given_ExceptionOccurs_When_Handle_Then_ReturnsProblemResult()
    {
        // Arrange
        var context = CreateInMemoryDbContext("a1b2c3d4-e5f6-7a8b-9c0d-e1f2a3b4c5da");
        var bookingId = Guid.Parse("12345678-1234-1234-1234-1234567890a6");
        var booking = new Booking
        {
            Id = bookingId,
            ItemId = Guid.Parse("12345678-1234-1234-1234-123456789077"),
            BorrowerId = Guid.Parse("12345678-1234-1234-1234-123456789078"),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1)
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        await context.DisposeAsync();

        var handler = new DeleteBookingHandler(context);
        var request = new DeleteBookingRequest(bookingId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var problemResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        problemResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}