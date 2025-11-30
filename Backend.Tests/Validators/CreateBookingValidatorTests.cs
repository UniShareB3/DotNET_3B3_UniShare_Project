using Backend.Data;
using Backend.Features.Booking;
using Backend.Features.Booking.DTO;
using Backend.Persistence;
using Backend.Validators;
using FluentAssertions;
using FluentValidation;
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
    
    
}


