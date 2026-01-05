using Backend.Data;
using Backend.Validators;
using Backend.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Backend.Tests;

// Tests for EmailValidator
public class EmailValidatorTests
{
    private readonly ApplicationContext _context;

    public EmailValidatorTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        _context = new ApplicationContext(options);

        _context.Universities.Add(new University
        {
            Id = Guid.NewGuid(),
            Name = "Test University",
            EmailDomain = "@student.uaic.ro"
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task Given_ValidEmailDomain_When_ValidatingEmail_Then_Success()
    {
        var universityId = _context.Universities.First().Id;
        var user = new User { Email = "myemail@student.uaic.ro", UniversityId = universityId };
        var userManager = GetMockUserManager();

        var validator = new EmailValidator(_context);
        var result = await validator.ValidateAsync(userManager, user);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Given_InvalidEmailDomain_When_ValidatingEmail_Then_InvalidEmailDomainError()
    {
        var universityId = _context.Universities.First().Id;
        var user = new User { Email = "myemail@email.com", UniversityId = universityId };
        var userManager = GetMockUserManager();

        var validator = new EmailValidator(_context);
        var result = await validator.ValidateAsync(userManager, user);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "InvalidEmailDomain");
    }

    [Fact]
    public async Task Given_NullEmail_When_ValidatingEmail_Then_InvalidEmailError()
    {
        var universityId = _context.Universities.First().Id;
        var user = new User { Email = null, UniversityId = universityId };
        var userManager = GetMockUserManager();

        var validator = new EmailValidator(_context);
        var result = await validator.ValidateAsync(userManager, user);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "InvalidEmail");
    }
    
    [Fact]
    public async Task Given_NullUniversityId_When_ValidatingEmail_Then_Success()
    {
        // Arrange
        var user = new User
        {
            Email = "stefan@uaic.ro",
            UniversityId = null
        };
        var userManager = GetMockUserManager();
        var validator = new EmailValidator(_context);
        
        // Act
        var result = await validator.ValidateAsync(userManager, user);
        
        // Assert
        Assert.True(result.Succeeded);
    }
    
    [Fact]
    public async Task Given_NonExistentUniversityId_When_ValidatingEmail_Then_ThrowsError()
    {
        // Arrange
        var universityId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var user = new User
        {
            Email = "stefan@uaic.ro",
            UniversityId = universityId
        };
        var userManager = GetMockUserManager();
        var validator = new EmailValidator(_context);

        // Act
        var result = await validator.ValidateAsync(userManager, user);
        
        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "UniversityNotFound");
    }


    private static UserManager<User> GetMockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new UserManager<User>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}