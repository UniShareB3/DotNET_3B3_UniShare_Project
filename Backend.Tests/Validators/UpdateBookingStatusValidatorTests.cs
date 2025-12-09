using Backend.Data;
using Backend.Features.Bookings;
using Backend.Features.Bookings.DTO;
using Backend.Features.Bookings.Enums;
using Backend.Persistence;
using Backend.Validators;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Validators;

public class UpdateBookingStatusValidatorTests
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
    public async Task Given_ValidRequest_When_Validate_Then_NoErrors()
    {
        // Arrange
        var context = CreateInMemoryDbContext("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var itemId = Guid.Parse("a3333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("a4444444-4444-4444-4444-444444444444");
        
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Test Item",
            Description = "A test item",
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };
        
        var booking = new Booking
        {
            Id = bookingId,
            ItemId = itemId,
            Item = item,
            BorrowerId = borrowerId,
            BookingStatus = BookingStatus.Pending,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(7)
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var validator = new UpdateBookingStatusValidator(context, logger);
        
        var dto = new UpdateBookingStatusDto(ownerId, BookingStatus.Approved);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public async Task Given_EmptyBookingId_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        var validator = new UpdateBookingStatusValidator(context, logger);
        
        var dto = new UpdateBookingStatusDto(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            BookingStatus.Approved
        );
        var request = new UpdateBookingStatusRequest(Guid.Empty, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BookingId);
    }
    
    [Fact]
    public async Task Given_NullBookingStatusDto_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        var validator = new UpdateBookingStatusValidator(context, logger);
        
        var request = new UpdateBookingStatusRequest(
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            null!
        );
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BookingStatusDto);
    }
    
    [Fact]
    public async Task Given_NonExistentBooking_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        var validator = new UpdateBookingStatusValidator(context, logger);
        
        var dto = new UpdateBookingStatusDto(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            BookingStatus.Approved
        );
        var request = new UpdateBookingStatusRequest(
            Guid.Parse("99999999-9999-9999-9999-999999999999"), // Non-existent booking
            dto
        );
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }
    
    [Fact]
    public async Task Given_UserNotOwner_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var nonOwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var itemId = Guid.Parse("e3333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("e4444444-4444-4444-4444-444444444444");
        
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Test Item",
            Description = "A test item",
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };
        
        var booking = new Booking
        {
            Id = bookingId,
            ItemId = itemId,
            Item = item,
            BorrowerId = borrowerId,
            BookingStatus = BookingStatus.Pending,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(7)
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var validator = new UpdateBookingStatusValidator(context, logger);
        
        // User is not the owner
        var dto = new UpdateBookingStatusDto(nonOwnerId, BookingStatus.Approved);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }
   
    
    [Fact]
    public async Task Given_BorrowerTriesToChangeStatus_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("i1i1i1i1-i1i1-i1i1-i1i1-i1i1i1i1i1i1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var itemId = Guid.Parse("99993333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("99994444-4444-4444-4444-444444444444");
        
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Test Item",
            Description = "A test item",
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };
        
        var booking = new Booking
        {
            Id = bookingId,
            ItemId = itemId,
            Item = item,
            BorrowerId = borrowerId,
            BookingStatus = BookingStatus.Pending,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(7)
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var validator = new UpdateBookingStatusValidator(context, logger);
        
        var dto = new UpdateBookingStatusDto(borrowerId, BookingStatus.Approved);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }
    
    [Fact]
    public async Task Given_AllFieldsInvalid_When_Validate_Then_ReturnsMultipleValidationErrors()
    {
        // Arrange
        var context = CreateInMemoryDbContext("j1j1j1j1-j1j1-j1j1-j1j1-j1j1j1j1j1j1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        var validator = new UpdateBookingStatusValidator(context, logger);
        
        var request = new UpdateBookingStatusRequest(Guid.Empty, null!);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BookingId);
        result.ShouldHaveValidationErrorFor(x => x.BookingStatusDto);
    }
}

