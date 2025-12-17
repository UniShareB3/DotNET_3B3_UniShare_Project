﻿﻿using AutoMapper;
using Backend.Data;
using Backend.Features.Bookings;
using Backend.Features.Bookings.DTO;
using Backend.Mapping;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

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

    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BookingMapper>(), NullLoggerFactory.Instance);
        return config.CreateMapper();
    }
 
    [Fact]
    public async Task Given_BookingsExist_When_Handle_Then_ReturnsAllBookings()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.Parse("10000000-0000-0000-0000-000000000001").ToString());
        var mapper = CreateMapper();
        
        var itemId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var itemId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        
        var item1 = new Item
        {
            Id = itemId1,
            Name = "Item 1",
            Description = "Description 1",
            OwnerId = Guid.Parse("33333333-3333-3333-3333-333333333333")
        };
        
        var item2 = new Item
        {
            Id = itemId2,
            Name = "Item 2",
            Description = "Description 2",
            OwnerId = Guid.Parse("44444444-4444-4444-4444-444444444444")
        };
        
        context.Items.AddRange(item1, item2);
        
        var booking1 = new Booking
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            ItemId = itemId1,
            BorrowerId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(2)
        };
        
        var booking2 = new Booking
        {
            Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
            ItemId = itemId2,
            BorrowerId = Guid.Parse("88888888-8888-8888-8888-888888888888"),
            StartDate = DateTime.UtcNow.AddDays(3),
            EndDate = DateTime.UtcNow.AddDays(5)
        };
        
        context.Bookings.AddRange(booking1, booking2);
        await context.SaveChangesAsync();
        
        var handler = new GetAllBookingsHandler(context, mapper);
        var request = new GetAllBookingsRequest();
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        var bookings = valueResult.Value.Should().BeAssignableTo<List<BookingDto>>().Subject;
        
        bookings.Should().HaveCount(2);
        bookings.Should().Contain(b => b.Id == booking1.Id);
        bookings.Should().Contain(b => b.Id == booking2.Id);
    }
    
    [Fact]
    public async Task Given_NoBookingsExist_When_Handle_Then_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.Parse("20000000-0000-0000-0000-000000000002").ToString());
        var mapper = CreateMapper();
        
        var handler = new GetAllBookingsHandler(context, mapper);
        var request = new GetAllBookingsRequest();
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        var bookings = valueResult.Value.Should().BeAssignableTo<List<BookingDto>>().Subject;
        
        bookings.Should().BeEmpty();
    }
    
    [Fact]
    public async Task Given_ExceptionOccurs_When_Handle_Then_ReturnsProblemResult()
    {
        // Arrange
        var mapper = CreateMapper();
        var handler = new GetAllBookingsHandler(null!, mapper);
        var request = new GetAllBookingsRequest();
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        var problemResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        problemResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}