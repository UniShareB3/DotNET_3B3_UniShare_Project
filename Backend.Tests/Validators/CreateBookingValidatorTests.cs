using Backend.Data;
using Backend.Features.Bookings;
using Backend.Features.Bookings.DTO;
using Backend.Persistence;
using Backend.Validators;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Validators;

/// <summary>
/// Example tests for a CreateBookingValidator if you implement one using FluentValidation.
/// Uncomment these tests after you create the validator in the Backend project.
/// </summary>
public class CreateBookingValidatorTests
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
    public async Task Given_NonExistentBorrower_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext("d1d2d3d4-d5d6-d7d8-d9d0-d1d2d3d4d5d6");
        var loggerValidator = new Mock<ILogger<CreateBookingValidator>>().Object;
        var validator = new CreateBookingValidator(context, loggerValidator);
        var itemId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        // Add item only, no user
        var item = new Item
        {
            Id = itemId,
            OwnerId = Guid.NewGuid(),
            Name = "Test Item",
            Description = "A test item",
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };
        
        context.Items.Add(item);
        await context.SaveChangesAsync();
        
        var bookingDto = new CreateBookingDto
        (
            itemId,
            Guid.NewGuid(), // Non-existent user
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(7)
        );
        
        var request = new CreateBookingRequest(bookingDto);
        
        // Act
        var resultValidator = await validator.TestValidateAsync(request);

        // Assert
        resultValidator.ShouldHaveValidationErrorFor(r => r.Booking.BorrowerId);
    }
    
        [Fact]
    public async Task Given_InvalidCreateBookingRequest_When_Handle_Then_ReturnsBadRequest()
    {
        // Arrange
        var context = CreateInMemoryDbContext("f1e2d3c4-b5a6-7b8c-9d0e-f1a2b3c4d5e6");
        var loggerValidator = new Mock<ILogger<CreateBookingValidator>>().Object;
        var validator = new CreateBookingValidator(context, loggerValidator);
        
        var invalidBookingDto = new CreateBookingDto
        (
            Guid.Parse("55555555-5555-5555-5555-555555555555"), // Non-existent ItemId
            Guid.Parse("55555555-5555-5555-5555-555555555558"), // Non-existent BorrowerId
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(1) // EndDate before StartDate
        );
        
        var request = new CreateBookingRequest(invalidBookingDto);
        
        // Act
        var resultValidator = await validator.TestValidateAsync(request);

        // Assert
        // The validator should find multiple validation errors
        resultValidator.ShouldHaveValidationErrorFor(r => r.Booking.BorrowerId);
        resultValidator.ShouldHaveValidationErrorFor(r => r.Booking.EndDate);
        resultValidator.ShouldHaveValidationErrorFor(r => r.Booking.StartDate);
    }
    
    [Fact]
    public async Task Given_EndDateBeforeStartDate_When_Handle_Then_ReturnsBadRequest()
    {
        // Arrange
        var context = CreateInMemoryDbContext("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var loggerValidator = new Mock<ILogger<CreateBookingValidator>>().Object;
        var validator = new CreateBookingValidator(context, loggerValidator);
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var itemId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Add user and item
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
            OwnerId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Name = "Test Item",
            Description = "A test item",
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };
        
        context.Users.Add(user);
        context.Items.Add(item);
        await context.SaveChangesAsync();
        
        var invalidBookingDto = new CreateBookingDto
        (
            itemId,
            userId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7),  // Start date is after
            DateTime.UtcNow.AddDays(1)   // End date 
        );
        
        var request = new CreateBookingRequest(invalidBookingDto);
        
        // Act
        var resultValidator = await validator.TestValidateAsync(request);

        // Assert
        resultValidator.ShouldHaveValidationErrorFor(r => r.Booking.EndDate);
        
    }
    
    [Fact]
    public async Task Given_NonExistentItem_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext("c1c2c3c4-c5c6-c7c8-c9c0-c1c2c3c4c5c6");
        var loggerValidator = new Mock<ILogger<CreateBookingValidator>>().Object;
        var validator = new CreateBookingValidator(context, loggerValidator);
        
        var userId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        // Add user only, no item
        var user = new User
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@student.uaic.ro"
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        var bookingDto = new CreateBookingDto
        (
            Guid.Parse("55555555-5555-5555-5555-555555555555"), 
            userId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(7)
        );
        
        var request = new CreateBookingRequest(bookingDto);
        
        // Act
        var resultValidator = await validator.TestValidateAsync(request);

        // Assert
        resultValidator.ShouldHaveValidationErrorFor(r => r.Booking.ItemId);
    }
    
}


