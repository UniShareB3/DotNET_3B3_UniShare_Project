using Backend.Data;
using Backend.Features.Bookings;
using Backend.Features.Bookings.DTO;
using Backend.Features.Bookings.Enums;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Handlers.Bookings;

public class UpdateBookingStatusHandlerTests
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
    public async Task Given_BookingExists_When_Handle_Then_UpdatesStatusAndReturnsOk()
    {
        // Arrange
        var logger = new Mock<ILogger<UpdateBookingStatusHandler>>().Object;
        var context = CreateInMemoryDbContext("a1b2c3d4-e5f6-7a8b-9c0d-e1f2a3a4c5d6" );
        var userId = Guid.Parse("bbcdefaa-cdef-abcd-efab-cdefabcdafab");
        var bookingId = Guid.Parse("bbcdefab-cdef-abcd-efab-cdefabcdefaa");
        var itemId = Guid.Parse("bbcdefab-cdef-abcd-efab-cdefabcdefab");
        var ownerId = Guid.Parse("ccccefab-cdef-abcd-efab-cdefabcdefab");
        
        var item = new Item
        {
            Id = itemId,
            Name = "Item",
            Description = "Description",
            OwnerId = ownerId,
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };
        context.Items.Add(item);
        
        var booking = new Booking
        {
            Id = bookingId,
            ItemId = itemId,
            BorrowerId = Guid.Parse("bbcdefab-cdef-abcd-efab-cdefabcdafab"),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(2),
            BookingStatus = BookingStatus.Pending
        };
        
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var handler = new UpdateBookingStatusHandler(context, logger);

        var dto = new UpdateBookingStatusDto(userId, BookingStatus.Approved);
       
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var updatedBooking = await context.Bookings.FindAsync(bookingId);
        updatedBooking.BookingStatus.Should().Be(BookingStatus.Approved);
    }
    
    [Fact]
    public async Task Given_BookingDoesNotExist_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var logger = new Mock<ILogger<UpdateBookingStatusHandler>>().Object;
        var context = CreateInMemoryDbContext("a1b2c3d4-e5f6-7a8b-9c0d-e1f2a9b4c5d6");
        var handler = new UpdateBookingStatusHandler(context, logger);
        var bookingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dto = new UpdateBookingStatusDto(Guid.Parse("22222222-2222-2222-2222-222222222222"), BookingStatus.Approved);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
    
    [Fact]
    public async Task Given_EmptyGuidBookingId_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var logger = new Mock<ILogger<UpdateBookingStatusHandler>>().Object;
        var context = CreateInMemoryDbContext("a1b2c3d4-e5f6-7a8b-9c0d-e1f2a3b005d6");
        var handler = new UpdateBookingStatusHandler(context, logger);
        var dto = new UpdateBookingStatusDto(Guid.Parse("33333333-3333-3333-3333-333333333333"), BookingStatus.Approved);
        var request = new UpdateBookingStatusRequest(Guid.Empty, dto);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
    
    [Fact]
    public async Task Given_SameStatusUpdate_When_Handle_Then_ReturnsOk()
    {
        // Arrange
        var logger = new Mock<ILogger<UpdateBookingStatusHandler>>().Object;
        var context = CreateInMemoryDbContext("a1b2c004-e5f6-7a8b-9c0d-e1f2a3b4c5d6");
        var bookingId = Guid.Parse("a1b2c004-e5f6-7a8b-9000-e1f2a3b4c5d6");
        var itemId = Guid.Parse("a1b2c004-e5f6-7a8b-000d-e1f2a3b4c5d6");
        
        var item = new Item
        {
            Id = itemId,
            Name = "Test Item",
            Description = "Test Description",
            OwnerId = Guid.Parse("00b2c004-e5f6-7a8b-9c0d-e1f2a3b4c5d6"),
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };
        context.Items.Add(item);
        
        var booking = new Booking
        {
            Id = bookingId,
            ItemId = itemId,
            BorrowerId = Guid.Parse("a1b2c004-e556-7a8b-9c0d-e1f2a3b4c5d6"),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(2),
            BookingStatus = BookingStatus.Approved
        };
        
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var handler = new UpdateBookingStatusHandler(context, logger);
        // Update to the same status
        var dto = new UpdateBookingStatusDto(Guid.Parse("a1b2c004-6666-7a8b-9c0d-e1f2a3b4c5d6"), BookingStatus.Approved);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var updatedBooking = await context.Bookings.FindAsync(bookingId);
        updatedBooking!.BookingStatus.Should().Be(BookingStatus.Approved);
    }
    
    [Fact]
    public async Task Given_BookingWithAllFields_When_Handle_Then_ReturnsCompleteBookingDto()
    {
        // Arrange
        var logger = new Mock<ILogger<UpdateBookingStatusHandler>>().Object;
        var context = CreateInMemoryDbContext("db-44444444-4444-4444-4444-444444444444");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var itemId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var borrowerId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var ownerId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var requestedOn = DateTime.UtcNow.AddDays(-1);
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(7);
        var approvedOn = DateTime.UtcNow;
        
        var item = new Item
        {
            Id = itemId,
            Name = "Complete Test Item",
            Description = "Test Description",
            OwnerId = ownerId,
            Category = Features.Items.Enums.ItemCategory.Books,
            Condition = Features.Items.Enums.ItemCondition.Excellent
        };
        context.Items.Add(item);
        
        var booking = new Booking
        {
            Id = bookingId,
            ItemId = itemId,
            BorrowerId = borrowerId,
            RequestedOn = requestedOn,
            StartDate = startDate,
            EndDate = endDate,
            BookingStatus = BookingStatus.Approved,
            ApprovedOn = approvedOn
        };
        
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var handler = new UpdateBookingStatusHandler(context, logger);
        var dto = new UpdateBookingStatusDto(Guid.Parse("88888888-8888-8888-8888-888888888888"), BookingStatus.Completed);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
    
    [Fact]
    public async Task Given_BookingWithCompletedOnDate_When_Handle_Then_PreservesCompletedOnDate()
    {
        // Arrange
        var logger = new Mock<ILogger<UpdateBookingStatusHandler>>().Object;
        var context = CreateInMemoryDbContext("db-99999999-9999-9999-9999-999999999999");
        var bookingId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var itemId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var completedOn = DateTime.UtcNow.AddDays(-1);
        
        var item = new Item
        {
            Id = itemId,
            Name = "Test Item",
            Description = "Test Description",
            OwnerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };
        context.Items.Add(item);
        
        var booking = new Booking
        {
            Id = bookingId,
            ItemId = itemId,
            BorrowerId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            StartDate = DateTime.UtcNow.AddDays(-5),
            EndDate = DateTime.UtcNow.AddDays(-2),
            BookingStatus = BookingStatus.Completed,
            CompletedOn = completedOn
        };
        
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var handler = new UpdateBookingStatusHandler(context, logger);
        var dto = new UpdateBookingStatusDto(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), BookingStatus.Canceled);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var updatedBooking = await context.Bookings.FindAsync(bookingId);
        updatedBooking!.CompletedOn.Should().Be(completedOn);
    }
    
    [Fact]
    public async Task Given_MultipleBookingsForSameItem_When_Handle_Then_UpdatesOnlyTargetBooking()
    {
        // Arrange
        var logger = new Mock<ILogger<UpdateBookingStatusHandler>>().Object;
        var context = CreateInMemoryDbContext("db-eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var targetBookingId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var otherBookingId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var itemId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        
        var item = new Item
        {
            Id = itemId,
            Name = "Shared Item",
            Description = "Test Description",
            OwnerId = Guid.Parse("11111111-2222-3333-4444-666666666666"),
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };
        context.Items.Add(item);
        
        var targetBooking = new Booking
        {
            Id = targetBookingId,
            ItemId = itemId,
            BorrowerId = Guid.Parse("11111111-2222-3333-4444-777777777777"),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(2),
            BookingStatus = BookingStatus.Pending
        };
        
        var otherBooking = new Booking
        {
            Id = otherBookingId,
            ItemId = itemId,
            BorrowerId = Guid.Parse("11111111-2222-3333-4444-888888888888"),
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(7),
            BookingStatus = BookingStatus.Pending
        };
        
        context.Bookings.Add(targetBooking);
        context.Bookings.Add(otherBooking);
        await context.SaveChangesAsync();
        
        var handler = new UpdateBookingStatusHandler(context, logger);
        var dto = new UpdateBookingStatusDto(Guid.Parse("11111111-2222-3333-4444-999999999999"), BookingStatus.Approved);
        var request = new UpdateBookingStatusRequest(targetBookingId, dto);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var updatedTargetBooking = await context.Bookings.FindAsync(targetBookingId);
        var unchangedOtherBooking = await context.Bookings.FindAsync(otherBookingId);
        
        updatedTargetBooking!.BookingStatus.Should().Be(BookingStatus.Approved);
        unchangedOtherBooking!.BookingStatus.Should().Be(BookingStatus.Pending);
    }
    
    [Fact]
    public async Task Given_BookingWithPastDates_When_Handle_Then_UpdatesStatusSuccessfully()
    {
        // Arrange
        var logger = new Mock<ILogger<UpdateBookingStatusHandler>>().Object;
        var context = CreateInMemoryDbContext("db-a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");
        var bookingId = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");
        var itemId = Guid.Parse("a2a2a2a2-a2a2-a2a2-a2a2-a2a2a2a2a2a2");
        
        var item = new Item
        {
            Id = itemId,
            Name = "Test Item",
            Description = "Test Description",
            OwnerId = Guid.Parse("a3a3a3a3-a3a3-a3a3-a3a3-a3a3a3a3a3a3"),
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };
        context.Items.Add(item);
        
        // Booking with dates in the past
        var booking = new Booking
        {
            Id = bookingId,
            ItemId = itemId,
            BorrowerId = Guid.Parse("a4a4a4a4-a4a4-a4a4-a4a4-a4a4a4a4a4a4"),
            RequestedOn = DateTime.UtcNow.AddDays(-30),
            StartDate = DateTime.UtcNow.AddDays(-20),
            EndDate = DateTime.UtcNow.AddDays(-10),
            BookingStatus = BookingStatus.Approved
        };
        
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var handler = new UpdateBookingStatusHandler(context, logger);
        var dto = new UpdateBookingStatusDto(Guid.Parse("a5a5a5a5-a5a5-a5a5-a5a5-a5a5a5a5a5a5"), BookingStatus.Completed);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var updatedBooking = await context.Bookings.FindAsync(bookingId);
        updatedBooking!.BookingStatus.Should().Be(BookingStatus.Completed);
    }
    
    [Fact]
    public async Task Given_BookingWithFutureDates_When_Handle_Then_UpdatesStatusSuccessfully()
    {
        // Arrange
        var logger = new Mock<ILogger<UpdateBookingStatusHandler>>().Object;
        var context = CreateInMemoryDbContext("db-b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1");
        var bookingId = Guid.Parse("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1");
        var itemId = Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2");
        
        var item = new Item
        {
            Id = itemId,
            Name = "Test Item",
            Description = "Test Description",
            OwnerId = Guid.Parse("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3"),
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };
        context.Items.Add(item);
        
        // Booking with dates in the future
        var booking = new Booking
        {
            Id = bookingId,
            ItemId = itemId,
            BorrowerId = Guid.Parse("b4b4b4b4-b4b4-b4b4-b4b4-b4b4b4b4b4b4"),
            RequestedOn = DateTime.UtcNow,
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(40),
            BookingStatus = BookingStatus.Pending
        };
        
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var handler = new UpdateBookingStatusHandler(context, logger);
        var dto = new UpdateBookingStatusDto(Guid.Parse("b5b5b5b5-b5b5-b5b5-b5b5-b5b5b5b5b5b5"), BookingStatus.Rejected);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var updatedBooking = await context.Bookings.FindAsync(bookingId);
        updatedBooking!.BookingStatus.Should().Be(BookingStatus.Rejected);
    }

    
    
}