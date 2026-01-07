using AutoMapper;
using Backend.Data;
using Backend.Features.Bookings.DTO;
using Backend.Features.Bookings.GetBooking;
using Backend.Mappers.Booking;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Backend.Tests.Handlers.Bookings;

public class GetBookingHandlerTests
{
    private static ApplicationContext CreateInMemoryDbContext(string guid)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: guid)
            .Options;

        var context = new ApplicationContext(options);
        return context;
    }

    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BookingMapper>(), NullLoggerFactory.Instance);
        return config.CreateMapper();
    }
    
    [Fact]
    public async Task Given_BookingExists_When_Handle_Then_ReturnsBooking()
    {
        // Arrange
        var context = CreateInMemoryDbContext("b1c2d3e4-f5a6-4789-8abc-def012345678");
        var logger = new Mock<ILogger<GetBookingHandler>>().Object;
        var mapper = CreateMapper();
        var bookingId = Guid.Parse("d4e5f6a7-b8c9-4def-0123-456789abcdef");
        var itemId = Guid.Parse("a1b2c3d4-e5f6-4789-8abc-def012345678");
        
        var item = new Item
        {
            Id = itemId,
            Name = "Test Item",
            Description = "Test Description",
            OwnerId = Guid.Parse("a1b2c3d4-e116-4789-8abc-def012345678")
        };
        context.Items.Add(item);
        
        var booking = new Booking
        {
            Id = bookingId,
            ItemId = itemId,
            BorrowerId = Guid.Parse("f1e2d3c4-b5a6-4789-8abc-def012345678"),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7)
        };

        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var handler = new GetBookingHandler(context, mapper, logger);
        var request = new GetBookingRequest(bookingId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        var returnedBooking = valueResult.Value.Should().BeAssignableTo<BookingDto>().Subject;

        returnedBooking.Id.Should().Be(booking.Id);
        returnedBooking.ItemId.Should().Be(booking.ItemId);
        returnedBooking.BorrowerId.Should().Be(booking.BorrowerId);
        returnedBooking.StartDate.Should().Be(booking.StartDate);
        returnedBooking.EndDate.Should().Be(booking.EndDate);
    }
    
    [Fact]
    public async Task Given_BookingDoesNotExist_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext("12345000-90ab-cdef-1234-567890abcdef");
        var logger = new Mock<ILogger<GetBookingHandler>>().Object;
        var mapper = CreateMapper();
        var handler = new GetBookingHandler(context, mapper, logger);
        var request = new GetBookingRequest(Guid.Parse("12345000-90ab-cdef-1234-567890abcdef"));

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}