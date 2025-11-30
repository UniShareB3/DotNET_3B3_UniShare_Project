using System.Net.Mime;
using Backend.Data;
using Backend.Persistence;
using Backend.Validators;
using FluentAssertions;
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
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var validator = new UniversityValidator(dbContext);

        var user = new User() 
        { 
            UniversityId = Guid.NewGuid(),
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
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var validUniversityId = Guid.NewGuid();

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
    
}