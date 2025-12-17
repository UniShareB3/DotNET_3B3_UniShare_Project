using Backend.Data;
using Backend.Persistence;
using Backend.Validators;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

public class UniversityValidatorTests
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
    public async Task Given_NonExistentUniversityId_When_Validate_Then_ReturnsUniversityNotFoundError()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext("4444-4444-4444-44444444444a");
        var validator = new UniversityValidator(dbContext);

        var user = new User() 
        { 
            UniversityId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Email = "test@example.com"
        };

        // Act
        var result = await validator.ValidateAsync(null!, user);

        // Assert
        result.Succeeded.Should().BeFalse();
    
        result.Errors.Should().Contain(e => e.Code == "UniversityNotFound");
        result.Errors.Should().Contain(e => e.Description == "The specified university does not exist.");
    }
    
    [Fact]
    public async Task Given_ValidExistingUniversityId_When_Validate_Then_ReturnsSuccess()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext("44444444-4444-4444-4444-4444444444aa");
        var validUniversityId = Guid.Parse("44444444-4444-4444-4444-444444444441");

        dbContext.Universities.Add(new University
        {
            Id = validUniversityId,
            Name = "Valid University",
            EmailDomain = "@university.edu"
        });
        await dbContext.SaveChangesAsync();

        var validator = new UniversityValidator(dbContext);

        var user = new User 
        { 
            UniversityId = validUniversityId,
            Email = "student@university.edu"
        };

        // Act
        var result = await validator.ValidateAsync(null!, user);

        // Assert
        result.Succeeded.Should().BeTrue();
    
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_EmptyUniversityId_When_Validate_Then_ReturnsInvalidUniversityError()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext("44444444-4444-4444-444a-444444444444");
        var validator = new UniversityValidator(dbContext);

        var user = new User
        {
            UniversityId = Guid.Empty,
            Email = "student@university.edu"
        };

        // Act
        var result = await validator.ValidateAsync(null!, user);
        
        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "InvalidUniversity");
        result.Errors.Should().Contain(e => e.Description == "The university ID is not set.");
    }

}