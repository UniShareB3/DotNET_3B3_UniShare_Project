using AutoMapper;
using Backend.Data;
using Backend.Features.Booking;
using Backend.Features.Booking.DTO;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Backend.Tests.Handlers.Bookings;

public class CreateBookingHandlerTests
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
        var mapperMock = new Mock<IMapper>();
        mapperMock
            .Setup(m => m.Map<Booking>(It.IsAny<CreateBookingDto>()))
            .Returns((Func<CreateBookingDto, Booking>)(src => new Booking
            {
                ItemId = src.ItemId,
                BorrowerId = src.BorrowerId,
                RequestedOn = src.RequestedOn,
                StartDate = src.StartDate,
                EndDate = src.EndDate
            }));

        return mapperMock.Object;
    }

    [Fact]
    public async Task Given_ValidCreateBookingRequest_When_Handle_Then_AddsNewBooking()
    {
        // Arrange
        var context = CreateInMemoryDbContext("b1c2d3e4-f5a6-7b8c-9d0e-f1a2b3c4d5e6");
        var mapper = CreateMapper();
        var handler = new CreateBookingHandler(context, mapper);
        var userId = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var itemId = Guid.Parse("abcdefab-cdef-abcd-efab-cdefabcdefab");

        // Add the user and item to the database
        var user = new User
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@student.uaic.ro"
        };

        var item = new Item
        {
            Id = itemId,
            OwnerId = Guid.NewGuid(),
            Name = "Test Item",
            Description = "A test item",
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };
        
        context.Users.Add(user);
        context.Items.Add(item);
        await context.SaveChangesAsync();
        
        var bookingDto = new CreateBookingDto
        (
            itemId,
            userId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(7)
        );
        
        var request = new CreateBookingRequest(bookingDto);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

}