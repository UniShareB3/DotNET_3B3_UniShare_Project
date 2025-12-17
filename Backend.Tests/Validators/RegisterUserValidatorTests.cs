using Backend.Data;
using Backend.Features.Users;
using Backend.Features.Users.Dtos;
using Backend.Persistence;
using Backend.Validators;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Validators;

public class RegisterUserValidatorTests
{
    private static ApplicationContext CreateInMemoryDbContext(string guid)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: guid)
            .Options;

        var dbContext = new ApplicationContext(options);
        return dbContext;
    }

    [Fact]
    public async Task Given_ExistingEmail_When_Validate_Then_ReturnsEmailAlreadyInUseError()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        var logger = new Mock<ILogger<RegisterUserValidator>>().Object;
       
        var existingEmail = "user@uaic.ro";
        dbContext.Users.Add(new User { Email = existingEmail });
        await dbContext.SaveChangesAsync();
        
        var validator = new RegisterUserValidator(dbContext, logger);

        RegisterUserDto dto = new RegisterUserDto
        (
            existingEmail,
            "Doe",
            "John",
            "ValidPass123",
            "Valid University"
        );

        var request = new RegisterUserRequest(dto);

        // Act
        var result = await validator.ValidateAsync(request);
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Email already in use.");
    }

    [Fact]
    public async Task Given_NonExistentUniversityName_When_Validate_Then_ReturnsUniversityNotFoundError()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext("44444444-4444-4444-4444-444444444224");
        var logger = new Mock<ILogger<RegisterUserValidator>>().Object;
        var validator = new RegisterUserValidator(dbContext, logger);

        RegisterUserDto dto = new RegisterUserDto
        (
            "user@uaic.ro",
            "Doe",
            "John",
            "ValidPass123",
            "Valid University"
        );
        var request = new RegisterUserRequest(dto);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "University does not exist.");
    }

    [Fact]
    public async Task Given_ValidRequest_When_Validate_Then_ReturnsSuccess()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext("22444444-4444-4444-4444-444444444444");
        var logger = new Mock<ILogger<RegisterUserValidator>>().Object;

        dbContext.Universities.Add(new University
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444411"),
            Name = "Valid University",
            EmailDomain = "@uaic.ro"
        });
        await dbContext.SaveChangesAsync();

        var validator = new RegisterUserValidator(dbContext, logger);

        RegisterUserDto dto = new RegisterUserDto
        (
            "user@uaic.ro",
            "Doe",
            "John",
            "ValidPass123",
            "Valid University"
        );

        var request = new RegisterUserRequest(dto);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
    
    [Fact]
    public async Task Given_InvalidPassword_When_Validate_Then_ReturnsPasswordValidationErrors()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext("44444444-4444-4444-4444-444444444422");
        var logger = new Mock<ILogger<RegisterUserValidator>>().Object;
        var validator = new RegisterUserValidator(dbContext, logger);
        
        dbContext.Universities.Add(new University
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444441"),
            Name = "Valid University",
            EmailDomain = "@uaic.ro"
        });
        await dbContext.SaveChangesAsync();

        RegisterUserDto dto = new RegisterUserDto
        (
            "user@uaic.ro",
            "Doe",
            "John",
            "Pass123",
            "Valid University"
        );
        
        var request = new RegisterUserRequest(dto);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Password must be at least 8 characters long."));
    }

    [Fact]
    public async Task Given_EmptyFirstName_When_Validate_Then_ReturnsFirstNameValidationError()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext("44444444-4444-4444-4444-444444444411");
        var logger = new Mock<ILogger<RegisterUserValidator>>().Object;
        var validator = new RegisterUserValidator(dbContext, logger);

        dbContext.Universities.Add(new University
        {
            Id = Guid.Parse("44444444-4444-4444-4433-444444444444"),
            Name = "Valid University",
            EmailDomain = "@uaic.ro"
        });
        await dbContext.SaveChangesAsync();

        RegisterUserDto dto = new RegisterUserDto
        (
            "user@uaic.ro",
            "",
            "John",
            "Pass123",
            "Valid University"
        );

        var request = new RegisterUserRequest(dto);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("FirstName is required."));
    }

    [Fact]
    public async Task Given_EmptyLastName_When_Validate_Then_ReturnsLastNameValidationError()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext("44444444-2444-4444-4444-444444444444");
        var logger = new Mock<ILogger<RegisterUserValidator>>().Object;
        var validator = new RegisterUserValidator(dbContext, logger);

        dbContext.Universities.Add(new University
        {
            Id = Guid.Parse("44444444-4444-2222-4444-444444444444"),
            Name = "Valid University",
            EmailDomain = "@uaic.ro"
        });
        await dbContext.SaveChangesAsync();

        RegisterUserDto dto = new RegisterUserDto
        (
            "user@uaic.ro",
            "John",
            "",
            "Pass123",
            "Valid University"
        );

        var request = new RegisterUserRequest(dto);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("LastName is required."));
    }


}