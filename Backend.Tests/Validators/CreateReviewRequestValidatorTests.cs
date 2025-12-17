using Backend.Data;
using Backend.Features.Bookings.Enums;
using Backend.Features.Review;
using Backend.Features.Review.DTO;
using Backend.Persistence;
using Backend.Validators;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Validators;

public class CreateReviewRequestValidatorTests
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
    public async Task Given_ValidReviewRequest_When_Validate_Then_NoErrors()
    {
        // Arrange
        var context = CreateInMemoryDbContext("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");
        var validator = new CreateReviewRequestValidator(context);
        
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
            BookingStatus = BookingStatus.Completed,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(-1)
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var reviewDto = new CreateReviewDTO(
            BookingId: bookingId,
            ReviewerId: borrowerId,
            TargetUserId: null,
            TargetItemId: itemId,
            Rating: 5,
            Comment: "Great item!",
            CreatedAt: DateTime.UtcNow
        );
        
        var request = new CreateReviewRequest(reviewDto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public async Task Given_RatingOutOfRange_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1");
        var validator = new CreateReviewRequestValidator(context);
        
        var reviewDto = new CreateReviewDTO(
            BookingId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
            ReviewerId: Guid.Parse("66666666-6666-6666-6666-666666666666"),
            TargetUserId: null,
            TargetItemId: Guid.Parse("77777777-7777-7777-7777-777777777777"),
            Rating: 6, // Invalid rating > 5
            Comment: "Test comment",
            CreatedAt: DateTime.UtcNow
        );
        
        var request = new CreateReviewRequest(reviewDto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Review.Rating);
    }
    
    [Fact]
    public async Task Given_EmptyReviewerId_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1");
        var validator = new CreateReviewRequestValidator(context);
        
        var reviewDto = new CreateReviewDTO(
            BookingId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
            ReviewerId: Guid.Empty, 
            TargetUserId: null,
            TargetItemId: Guid.Parse("77777777-7777-7777-7777-777777777777"),
            Rating: 4,
            Comment: "Test comment",
            CreatedAt: DateTime.UtcNow
        );
        
        var request = new CreateReviewRequest(reviewDto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Review.ReviewerId);
    }
    
    [Fact]
    public async Task Given_EmptyBookingId_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1");
        var validator = new CreateReviewRequestValidator(context);
        
        var reviewDto = new CreateReviewDTO(
            BookingId: Guid.Empty, // Invalid empty GUID
            ReviewerId: Guid.Parse("66666666-6666-6666-6666-666666666666"),
            TargetUserId: null,
            TargetItemId: Guid.Parse("77777777-7777-7777-7777-777777777777"),
            Rating: 4,
            Comment: "Test comment",
            CreatedAt: DateTime.UtcNow
        );
        
        var request = new CreateReviewRequest(reviewDto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Review.BookingId);
    }
    
    [Fact]
    public async Task Given_BothTargetItemAndTargetUser_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1");
        var validator = new CreateReviewRequestValidator(context);


        var reviewDto = new CreateReviewDTO(
            BookingId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
            ReviewerId: Guid.Parse("66666666-6666-6666-6666-666666666666"),
            TargetUserId: Guid.Parse("88888888-8888-8888-8888-888888888888"), 
            TargetItemId: Guid.Parse("77777777-7777-7777-7777-777777777777"), 
            Rating: 4,
            Comment: "Test comment",
            CreatedAt: DateTime.UtcNow
        );
        var request = new CreateReviewRequest(reviewDto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Review);
    }
    
    [Fact]
    public async Task Given_NeitherTargetItemNorTargetUser_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("g1g1g1g1-g1g1-g1g1-g1g1-g1g1g1g1g1g1");
        var validator = new CreateReviewRequestValidator(context);
        
        var reviewDto = new CreateReviewDTO(
            BookingId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
            ReviewerId: Guid.Parse("66666666-6666-6666-6666-666666666666"),
            TargetUserId: null, 
            TargetItemId: null,
            Rating: 4,
            Comment: "Test comment",
            CreatedAt: DateTime.UtcNow
        );
        
        var request = new CreateReviewRequest(reviewDto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Review);
    }
    
    [Fact]
    public async Task Given_NonExistentBooking_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("h1h1h1h1-h1h1-h1h1-h1h1-h1h1h1h1h1h1");
        var validator = new CreateReviewRequestValidator(context);
        
        var reviewDto = new CreateReviewDTO(
            BookingId: Guid.Parse("99999999-9999-9999-9999-999999999999"), // Non-existent
            ReviewerId: Guid.Parse("66666666-6666-6666-6666-666666666666"),
            TargetUserId: null,
            TargetItemId: Guid.Parse("77777777-7777-7777-7777-777777777777"),
            Rating: 4,
            Comment: "Test comment",
            CreatedAt: DateTime.UtcNow
        );
        
        var request = new CreateReviewRequest(reviewDto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor("BookingId");
    }
    
    [Fact]
    public async Task Given_BookingNotCompleted_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("i1i1i1i1-i1i1-i1i1-i1i1-i1i1i1i1i1i1");
        var validator = new CreateReviewRequestValidator(context);
        
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
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(-1)
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var reviewDto = new CreateReviewDTO(
            BookingId: bookingId,
            ReviewerId: borrowerId,
            TargetUserId: null,
            TargetItemId: itemId,
            Rating: 5,
            Comment: "Great item!",
            CreatedAt: DateTime.UtcNow
        );
        
        var request = new CreateReviewRequest(reviewDto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor("BookingId");
    }
    
    [Fact]
    public async Task Given_ReviewerNotBorrowerOrOwner_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("j1j1j1j1-j1j1-j1j1-j1j1-j1j1j1j1j1j1");
        var validator = new CreateReviewRequestValidator(context);
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var unrelatedUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
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
            BookingStatus = BookingStatus.Completed,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(-1)
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var reviewDto = new CreateReviewDTO(
            BookingId: bookingId,
            ReviewerId: unrelatedUserId,
            TargetUserId: null,
            TargetItemId: itemId,
            Rating: 5,
            Comment: "Great item!",
            CreatedAt: DateTime.UtcNow
        );
        
        var request = new CreateReviewRequest(reviewDto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor("ReviewerId");
    }
    
    [Fact]
    public async Task Given_DuplicateReview_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("k1k1k1k1-k1k1-k1k1-k1k1-k1k1k1k1k1k1");
        var validator = new CreateReviewRequestValidator(context);
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var bookingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var existingReviewId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        
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
            BookingStatus = BookingStatus.Completed,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(-1)
        };
        
        var existingReview = new Review
        {
            Id = existingReviewId,
            BookingId = bookingId,
            ReviewerId = borrowerId,
            TargetItemId = itemId,
            Rating = 4,
            Comment = "Existing review"
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        context.Reviews.Add(existingReview);
        await context.SaveChangesAsync();
        
        var reviewDto = new CreateReviewDTO(
            BookingId: bookingId,
            ReviewerId: borrowerId, // Same reviewer
            TargetUserId: null,
            TargetItemId: itemId,
            Rating: 5,
            Comment: "Duplicate review!",
            CreatedAt: DateTime.UtcNow
        );
        
        var request = new CreateReviewRequest(reviewDto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor("BookingId");
    }
    
    [Fact]
    public async Task Given_OwnerReviewingItem_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("l1l1l1l1-l1l1-l1l1-l1l1-l1l1l1l1l1l1");
        var validator = new CreateReviewRequestValidator(context);
        
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
            BookingStatus = BookingStatus.Completed,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(-1)
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var reviewDto = new CreateReviewDTO(
            BookingId: bookingId,
            ReviewerId: ownerId, 
            TargetUserId: null,
            TargetItemId: itemId, 
            Rating: 5,
            Comment: "Great item!",
            CreatedAt: DateTime.UtcNow
        );
        
        var request = new CreateReviewRequest(reviewDto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor("ReviewerId");
    }
    
    [Fact]
    public async Task Given_TargetItemIdMismatch_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("m1m1m1m1-m1m1-m1m1-m1m1-m1m1m1m1m1m1");
        var validator = new CreateReviewRequestValidator(context);
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var itemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var differentItemId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
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
            BookingStatus = BookingStatus.Completed,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(-1)
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var reviewDto = new CreateReviewDTO(
            BookingId: bookingId,
            ReviewerId: borrowerId,
            TargetUserId: null,
            TargetItemId: differentItemId, 
            Rating: 5,
            Comment: "Great item!",
            CreatedAt: DateTime.UtcNow
        );
        
        var request = new CreateReviewRequest(reviewDto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor("TargetItemId");
    }
    
    [Fact]
    public async Task Given_TargetUserIdMismatch_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("n1n1n1n1-n1n1-n1n1-n1n1-n1n1n1n1n1n1");
        var validator = new CreateReviewRequestValidator(context);
        
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var borrowerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var unrelatedUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
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
            BookingStatus = BookingStatus.Completed,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(-1)
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var reviewDto = new CreateReviewDTO(
            BookingId: bookingId,
            ReviewerId: borrowerId,
            TargetUserId: unrelatedUserId, 
            TargetItemId: null,
            Rating: 5,
            Comment: "Great owner!",
            CreatedAt: DateTime.UtcNow
        );
        
        var request = new CreateReviewRequest(reviewDto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor("TargetUserId");
    }
    
    [Fact]
    public async Task Given_OwnerReviewingBorrower_When_Validate_Then_NoErrors()
    {
        // Arrange
        var context = CreateInMemoryDbContext("o1o1o1o1-o1o1-o1o1-o1o1-o1o1o1o1o1o1");
        var validator = new CreateReviewRequestValidator(context);
        
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
            BookingStatus = BookingStatus.Completed,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(-1)
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var reviewDto = new CreateReviewDTO(
            BookingId: bookingId,
            ReviewerId: ownerId, 
            TargetUserId: borrowerId, 
            TargetItemId: null,
            Rating: 5,
            Comment: "Great borrower!",
            CreatedAt: DateTime.UtcNow
        );
        
        var request = new CreateReviewRequest(reviewDto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public async Task Given_BorrowerReviewingOwner_When_Validate_Then_NoErrors()
    {
        // Arrange
        var context = CreateInMemoryDbContext("p1p1p1p1-p1p1-p1p1-p1p1-p1p1p1p1p1p1");
        var validator = new CreateReviewRequestValidator(context);
        
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
            BookingStatus = BookingStatus.Completed,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(-1)
        };
        
        context.Items.Add(item);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        var reviewDto = new CreateReviewDTO(
            BookingId: bookingId,
            ReviewerId: borrowerId, 
            TargetUserId: ownerId, 
            TargetItemId: null,
            Rating: 5,
            Comment: "Great owner!",
            CreatedAt: DateTime.UtcNow
        );
        
        var request = new CreateReviewRequest(reviewDto);
        
        // Act
        var result = await validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}