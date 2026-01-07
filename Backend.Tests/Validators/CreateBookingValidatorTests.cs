using Backend.Data;
using Backend.Features.Bookings.CreateBooking;
using Backend.Features.Bookings.DTO;
using Backend.Persistence;
using Backend.Validators;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Validators;

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
        var context = CreateInMemoryDbContext("create-booking-" + Guid.NewGuid());
        var loggerValidator = new Mock<ILogger<CreateBookingValidator>>().Object;
        var validator = new CreateBookingValidator(context, loggerValidator);
        var itemId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
        var item = new Item
        {
            Id = itemId,
            OwnerId = Guid.Parse("44444444-4444-4444-4444-444444444411"),
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
            Guid.Parse("44444444-4444-4444-4444-444444444422"),
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
        var context = CreateInMemoryDbContext("create-booking-" + Guid.NewGuid());
        var loggerValidator = new Mock<ILogger<CreateBookingValidator>>().Object;
        var validator = new CreateBookingValidator(context, loggerValidator);
        
        var invalidBookingDto = new CreateBookingDto
        (
            Guid.Parse("55555555-5555-5555-5555-555555555555"), 
            Guid.Parse("55555555-5555-5555-5555-555555555558"), 
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(1) 
        );
        
        var request = new CreateBookingRequest(invalidBookingDto);
        
        // Act
        var resultValidator = await validator.TestValidateAsync(request);

        // Assert
        resultValidator.ShouldHaveValidationErrorFor(r => r.Booking.BorrowerId);
        resultValidator.ShouldHaveValidationErrorFor(r => r.Booking.EndDate);
        resultValidator.ShouldHaveValidationErrorFor(r => r.Booking.StartDate);
    }
    
    [Fact]
    public async Task Given_EndDateBeforeStartDate_When_Handle_Then_ReturnsBadRequest()
    {
        // Arrange
        var context = CreateInMemoryDbContext("create-booking-" + Guid.NewGuid());
        var loggerValidator = new Mock<ILogger<CreateBookingValidator>>().Object;
        var validator = new CreateBookingValidator(context, loggerValidator);
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var itemId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        
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
            DateTime.UtcNow.AddDays(7),  
            DateTime.UtcNow.AddDays(1)   
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
        var context = CreateInMemoryDbContext("create-booking-" + Guid.NewGuid());
        var loggerValidator = new Mock<ILogger<CreateBookingValidator>>().Object;
        var validator = new CreateBookingValidator(context, loggerValidator);
        
        var userId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
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


