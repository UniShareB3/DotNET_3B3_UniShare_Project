using Backend.Data;
using Backend.Features.Bookings.DTO;
using Backend.Features.Bookings.Enums;
using Backend.Features.Bookings.UpdateBooking;
using Backend.Persistence;
using Backend.Validators;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Validators.Bookings;

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
    
    #region Completed Status Tests
    
    [Fact]
    public async Task Given_OwnerMarksAsCompleted_When_Validate_Then_NoErrors()
    {
        // Arrange
        var context = CreateInMemoryDbContext("comp-owner-1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
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
            BookingStatus = BookingStatus.Approved,
            StartDate = DateTime.UtcNow.AddDays(-2),
            EndDate = DateTime.UtcNow.AddDays(5)
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var validator = new UpdateBookingStatusValidator(context, logger);
        var dto = new UpdateBookingStatusDto(ownerId, BookingStatus.Completed);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public async Task Given_BorrowerMarksAsCompleted_When_Validate_Then_NoErrors()
    {
        // Arrange
        var context = CreateInMemoryDbContext("comp-borrower-1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
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
            BookingStatus = BookingStatus.Approved,
            StartDate = DateTime.UtcNow.AddDays(-2),
            EndDate = DateTime.UtcNow.AddDays(5)
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var validator = new UpdateBookingStatusValidator(context, logger);
        var dto = new UpdateBookingStatusDto(ownerId, BookingStatus.Completed);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public async Task Given_NonOwnerNonBorrowerMarksAsCompleted_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("comp-unauthorized-1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var unauthorizedUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
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
            BookingStatus = BookingStatus.Approved,
            StartDate = DateTime.UtcNow.AddDays(-2),
            EndDate = DateTime.UtcNow.AddDays(5)
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var validator = new UpdateBookingStatusValidator(context, logger);
        var dto = new UpdateBookingStatusDto(unauthorizedUserId, BookingStatus.Completed);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }
    
    #endregion
    
    #region Canceled Status Tests
    
    [Fact]
    public async Task Given_OwnerCancelsPendingBooking_When_Validate_Then_NoErrors()
    {
        // Arrange
        var context = CreateInMemoryDbContext("cancel-owner-pending-1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
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
        var dto = new UpdateBookingStatusDto(ownerId, BookingStatus.Canceled);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public async Task Given_OwnerCancelsApprovedBooking_When_Validate_Then_NoErrors()
    {
        // Arrange
        var context = CreateInMemoryDbContext("cancel-owner-approved-1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
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
            BookingStatus = BookingStatus.Approved,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(7)
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var validator = new UpdateBookingStatusValidator(context, logger);
        var dto = new UpdateBookingStatusDto(ownerId, BookingStatus.Canceled);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public async Task Given_BorrowerCancelsPendingBooking_When_Validate_Then_NoErrors()
    {
        // Arrange
        var context = CreateInMemoryDbContext("cancel-borrower-pending-1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
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
        var dto = new UpdateBookingStatusDto(ownerId, BookingStatus.Canceled);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public async Task Given_BorrowerCancelsApprovedBooking_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("cancel-borrower-approved-1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
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
            BookingStatus = BookingStatus.Approved,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(7)
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var validator = new UpdateBookingStatusValidator(context, logger);
        var dto = new UpdateBookingStatusDto(borrowerId, BookingStatus.Canceled);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }
    
    [Fact]
    public async Task Given_BorrowerCancelsRejectedBooking_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("cancel-borrower-rejected-1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
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
            BookingStatus = BookingStatus.Rejected,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(7)
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var validator = new UpdateBookingStatusValidator(context, logger);
        var dto = new UpdateBookingStatusDto(borrowerId, BookingStatus.Canceled);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }
    
    [Fact]
    public async Task Given_UnauthorizedUserCancelsBooking_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("cancel-unauthorized-1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var unauthorizedUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
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
        var dto = new UpdateBookingStatusDto(unauthorizedUserId, BookingStatus.Canceled);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }
    
    #endregion
    
    #region Other Status Tests (Approved, Rejected)
    
    [Fact]
    public async Task Given_OwnerApprovesBooking_When_Validate_Then_NoErrors()
    {
        // Arrange
        var context = CreateInMemoryDbContext("approve-owner-1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
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
    public async Task Given_OwnerRejectsBooking_When_Validate_Then_NoErrors()
    {
        // Arrange
        var context = CreateInMemoryDbContext("reject-owner-1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
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
        var dto = new UpdateBookingStatusDto(ownerId, BookingStatus.Rejected);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public async Task Given_BorrowerTriesToApproveBooking_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("approve-borrower-1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
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
    public async Task Given_BorrowerTriesToRejectBooking_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("reject-borrower-1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
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
        var dto = new UpdateBookingStatusDto(borrowerId, BookingStatus.Rejected);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }
    
    #endregion
    
    #region Edge Cases
    
    [Fact]
    public async Task Given_BookingWithoutItemInMemory_When_Validate_Then_LoadsItemFromDatabase()
    {
        // Arrange - Test the case where Item is not loaded with booking
        var context = CreateInMemoryDbContext("edge-no-item-1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Test Item",
            Description = "A test item",
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };
        
        context.Items.Add(item);
        
        // Create booking without Item navigation property loaded
        var booking = new Booking
        {
            Id = bookingId,
            ItemId = itemId,
            Item = null, // Explicitly set to null to test fallback
            BorrowerId = borrowerId,
            BookingStatus = BookingStatus.Pending,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(7)
        };
        
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        // Detach to ensure Item is not loaded
        context.Entry(booking).State = EntityState.Detached;
        
        var validator = new UpdateBookingStatusValidator(context, logger);
        var dto = new UpdateBookingStatusDto(ownerId, BookingStatus.Approved);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public async Task Given_BookingWithNonExistentItem_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("edge-missing-item-1");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
        // Create booking without the corresponding item
        var booking = new Booking
        {
            Id = bookingId,
            ItemId = itemId,
            Item = null,
            BorrowerId = borrowerId,
            BookingStatus = BookingStatus.Pending,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(7)
        };
        
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var validator = new UpdateBookingStatusValidator(context, logger);
        var dto = new UpdateBookingStatusDto(Guid.NewGuid(), BookingStatus.Approved);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }
    
    #endregion
    
    #region Custom Validation Tests
    
    [Fact]
    public async Task Given_NonExistentBooking_When_ValidatingOwnership_Then_ReturnsBookingNotFoundError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("aaaaaaaa-1111-2222-3333-444444444444");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        var validator = new UpdateBookingStatusValidator(context, logger);
        
        var nonExistentBookingId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dto = new UpdateBookingStatusDto(userId, BookingStatus.Approved);
        var request = new UpdateBookingStatusRequest(nonExistentBookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Booking does not exist");
    }
    
    [Fact]
    public async Task Given_BookingWithNonExistentItem_When_ValidatingOwnership_Then_ReturnsItemNotFoundError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("bbbbbbbb-2222-3333-4444-555555555555");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var bookingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var nonExistentItemId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        
        var booking = new Booking
        {
            Id = bookingId,
            ItemId = nonExistentItemId,
            BorrowerId = borrowerId,
            BookingStatus = BookingStatus.Pending,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(7)
        };
        
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
    public async Task Given_UserNotOwnerOrBorrower_When_ValidatingOwnership_Then_ReturnsUnauthorizedError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("cccccccc-3333-4444-5555-666666666666");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var unauthorizedUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
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
        var dto = new UpdateBookingStatusDto(unauthorizedUserId, BookingStatus.Approved);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("User must be either the borrower or the owner of the item to update booking status");
    }
    
    [Fact]
    public async Task Given_BorrowerCancellingApprovedBooking_When_ValidatingOwnership_Then_ReturnsBorrowerCanOnlyCancelPendingError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("dddddddd-4444-5555-6666-777777777777");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
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
            BookingStatus = BookingStatus.Approved, // Already approved, not pending
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(7)
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var validator = new UpdateBookingStatusValidator(context, logger);
        var dto = new UpdateBookingStatusDto(borrowerId, BookingStatus.Canceled);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Borrower can only cancel a booking when it is still Pending.");
    }
    
    [Fact]
    public async Task Given_UserDifferentFromOwnerAndBorrower_When_ValidatingCancelStatus_Then_ReturnsOnlyOwnerOrBorrowerError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("eeeeeeee-5555-6666-7777-888888888888");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var differentUserId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
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
        // User ID is neither owner nor borrower
        var dto = new UpdateBookingStatusDto(differentUserId, BookingStatus.Canceled);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("User must be either the borrower or the owner of the item to update booking status");
    }
    
    [Fact]
    public async Task Given_BookingWithDeletedItem_When_ValidatingItemExists_Then_ReturnsItemNotFoundError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("ffffffff-6666-7777-8888-999999999999");
        var logger = new Mock<ILogger<UpdateBookingStatusValidator>>().Object;
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var deletedItemId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
        // Create a booking without adding the corresponding item to the database
        // This simulates a scenario where the item was deleted but booking still references it
        var booking = new Booking
        {
            Id = bookingId,
            ItemId = deletedItemId,
            BorrowerId = borrowerId,
            BookingStatus = BookingStatus.Pending,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(7)
        };
        
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var validator = new UpdateBookingStatusValidator(context, logger);
        var dto = new UpdateBookingStatusDto(ownerId, BookingStatus.Approved);
        var request = new UpdateBookingStatusRequest(bookingId, dto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Booking does not exist");
    }
    
    #endregion
}
