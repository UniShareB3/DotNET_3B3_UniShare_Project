using Backend.Data;
using Backend.Features.Shared.IAM.ChangePassword;
using Backend.Features.Shared.IAM.DTO;
using Backend.Persistence;
using Backend.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Guid = System.Guid;

namespace Backend.Tests.Validators;

public class ChangePasswordRequestTests
{
    private static ApplicationContext CreateInMemoryDbContext(string guid)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: guid)
            .Options;

        var context = new ApplicationContext(options);
        return context;
    }
    
    private static UserManager<User> CreateFilledUserManager(ApplicationContext context)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityOptions
        {
            Password = new PasswordOptions
            {
                RequiredLength = 6,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireNonAlphanumeric = true
            }
        });

        var store = new UserStore<User, IdentityRole<Guid>, ApplicationContext, Guid>(context);
        var userValidators = new List<IUserValidator<User>> { new UserValidator<User>() };
        var passwordValidators = new List<IPasswordValidator<User>> { 
            new PasswordValidator<User>()
        };
        
        var mockLogger = new Mock<ILogger<UserManager<User>>>();
        var passwordHasher = new PasswordHasher<User>();
        
        var userManager = new UserManager<User>(
            store,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null,
            mockLogger.Object);
        
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            Email = "testuser@uaic.ro"
        };

        userManager.CreateAsync(user, "ExistingP@ssw0rd").Wait();
        
        return userManager;
    }
    
    [Fact]
    private async Task Given_ChangePasswordRequest_When_Valid_Then_PassesValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("change-password-request-" + Guid.NewGuid());
        UserManager<User> mockUserManager = CreateFilledUserManager(context);
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        ChangePasswordRequest request = new ChangePasswordRequest(new ChangePasswordDto("StrongP@ssw0rd", userId));
        ChangePasswordRequestValidator validator = new ChangePasswordRequestValidator(mockUserManager);
        
        // Act
        var resultValidator = await validator.ValidateAsync(request);
        
        // Assert
        Assert.True(resultValidator.IsValid);
    }
    
    [Fact]
    public async Task Given_ChangePasswordRequest_When_NullDto_Then_FailsValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("test-null-dto");
        var mockUserManager = CreateFilledUserManager(context);
        var request = new ChangePasswordRequest(null!);
        var validator = new ChangePasswordRequestValidator(mockUserManager);
        
        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(async () => await validator.ValidateAsync(request));
    }
    
    [Fact]
    public async Task Given_ChangePasswordRequest_When_EmptyUserId_Then_FailsValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("test-empty-userid");
        UserManager<User> mockUserManager = CreateFilledUserManager(context);
        ChangePasswordRequest request = new ChangePasswordRequest(new ChangePasswordDto("StrongP@ssw0rd", Guid.Empty));
        ChangePasswordRequestValidator validator = new ChangePasswordRequestValidator(mockUserManager);
        
        // Act
        var resultValidator = await validator.ValidateAsync(request);
        
        // Assert
        Assert.False(resultValidator.IsValid);
        Assert.Contains(resultValidator.Errors, e => e.ErrorMessage == "User ID is required.");
    }
    
    [Fact]
    public async Task Given_ChangePasswordRequest_When_EmptyPassword_Then_FailsValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("test-empty-password");
        UserManager<User> mockUserManager = CreateFilledUserManager(context);
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        ChangePasswordRequest request = new ChangePasswordRequest(new ChangePasswordDto("", userId));
        ChangePasswordRequestValidator validator = new ChangePasswordRequestValidator(mockUserManager);
        
        // Act
        var resultValidator = await validator.ValidateAsync(request);
        
        // Assert
        Assert.False(resultValidator.IsValid);
        Assert.Contains(resultValidator.Errors, e => e.ErrorMessage == "New password is required.");
    }
    
    [Fact]
    public async Task Given_ChangePasswordRequest_When_PasswordTooShort_Then_FailsValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("test-short-password");
        UserManager<User> mockUserManager = CreateFilledUserManager(context);
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        ChangePasswordRequest request = new ChangePasswordRequest(new ChangePasswordDto("Ab1!", userId));
        ChangePasswordRequestValidator validator = new ChangePasswordRequestValidator(mockUserManager);
        
        // Act
        var resultValidator = await validator.ValidateAsync(request);
        
        // Assert
        Assert.False(resultValidator.IsValid);
        Assert.Contains(resultValidator.Errors, e => e.ErrorMessage == "Password must be at least 6 characters long.");
    }
    
    [Fact]
    public async Task Given_ChangePasswordRequest_When_PasswordMissingDigit_Then_FailsValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("test-no-digit");
        UserManager<User> mockUserManager = CreateFilledUserManager(context);
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        ChangePasswordRequest request = new ChangePasswordRequest(new ChangePasswordDto("StrongP@ssword", userId));
        ChangePasswordRequestValidator validator = new ChangePasswordRequestValidator(mockUserManager);
        
        // Act
        var resultValidator = await validator.ValidateAsync(request);
        
        // Assert
        Assert.False(resultValidator.IsValid);
        Assert.Contains(resultValidator.Errors, e => e.ErrorMessage.Contains("digit"));
    }
    
    [Fact]
    public async Task Given_ChangePasswordRequest_When_PasswordMissingUppercase_Then_FailsValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("test-no-uppercase");
        UserManager<User> mockUserManager = CreateFilledUserManager(context);
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        ChangePasswordRequest request = new ChangePasswordRequest(new ChangePasswordDto("strongp@ssw0rd", userId));
        ChangePasswordRequestValidator validator = new ChangePasswordRequestValidator(mockUserManager);
        
        // Act
        var resultValidator = await validator.ValidateAsync(request);
        
        // Assert
        Assert.False(resultValidator.IsValid);
        Assert.Contains(resultValidator.Errors, e => e.ErrorMessage.Contains("uppercase"));
    }
    
    [Fact]
    public async Task Given_ChangePasswordRequest_When_PasswordMissingLowercase_Then_FailsValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("test-no-lowercase");
        UserManager<User> mockUserManager = CreateFilledUserManager(context);
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        ChangePasswordRequest request = new ChangePasswordRequest(new ChangePasswordDto("STRONGP@SSW0RD", userId));
        ChangePasswordRequestValidator validator = new ChangePasswordRequestValidator(mockUserManager);
        
        // Act
        var resultValidator = await validator.ValidateAsync(request);
        
        // Assert
        Assert.False(resultValidator.IsValid);
        Assert.Contains(resultValidator.Errors, e => e.ErrorMessage.Contains("lowercase"));
    }
    
    [Fact]
    public async Task Given_ChangePasswordRequest_When_PasswordMissingSpecialChar_Then_FailsValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("test-no-special");
        UserManager<User> mockUserManager = CreateFilledUserManager(context);
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        ChangePasswordRequest request = new ChangePasswordRequest(new ChangePasswordDto("StrongPassw0rd", userId));
        ChangePasswordRequestValidator validator = new ChangePasswordRequestValidator(mockUserManager);
        
        // Act
        var resultValidator = await validator.ValidateAsync(request);
        
        // Assert
        Assert.False(resultValidator.IsValid);
        Assert.Contains(resultValidator.Errors, e => e.ErrorMessage.Contains("alphanumeric"));
    }
    
    [Fact]
    public async Task Given_ChangePasswordRequest_When_UserNotFound_Then_FailsValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("test-user-not-found");
        UserManager<User> mockUserManager = CreateFilledUserManager(context);
        Guid nonExistentUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        ChangePasswordRequest request = new ChangePasswordRequest(new ChangePasswordDto("StrongP@ssw0rd", nonExistentUserId));
        ChangePasswordRequestValidator validator = new ChangePasswordRequestValidator(mockUserManager);
        
        // Act
        var resultValidator = await validator.ValidateAsync(request);
        
        // Assert
        Assert.False(resultValidator.IsValid);
        Assert.Contains(resultValidator.Errors, e => e.ErrorMessage == "User not found.");
    }
}