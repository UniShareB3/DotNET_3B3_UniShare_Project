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
    public async Task Given_ValidRequest_When_Validating_Then_PassesValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
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
    public async Task Given_NullDto_When_Validating_Then_ThrowsNullReferenceException()
    {
        // Arrange
        var context = CreateInMemoryDbContext("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var mockUserManager = CreateFilledUserManager(context);
        var request = new ChangePasswordRequest(null!);
        var validator = new ChangePasswordRequestValidator(mockUserManager);
        
        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(async () => await validator.ValidateAsync(request));
    }
    
    [Fact]
    public async Task Given_EmptyUserId_When_Validating_Then_FailsValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("cccccccc-cccc-cccc-cccc-cccccccccccc");
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
    public async Task Given_EmptyPassword_When_Validating_Then_FailsValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("dddddddd-dddd-dddd-dddd-dddddddddddd");
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
    public async Task Given_PasswordTooShort_When_Validating_Then_FailsValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
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
    public async Task Given_PasswordMissingDigit_When_Validating_Then_FailsValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("ffffffff-ffff-ffff-ffff-ffffffffffff");
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
    public async Task Given_PasswordMissingUppercase_When_Validating_Then_FailsValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("11111111-2222-3333-4444-555555555555");
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
    public async Task Given_PasswordMissingLowercase_When_Validating_Then_FailsValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("22222222-3333-4444-5555-666666666666");
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
    public async Task Given_PasswordMissingSpecialChar_When_Validating_Then_FailsValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("33333333-4444-5555-6666-777777777777");
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
    public async Task Given_NonExistentUser_When_Validating_Then_FailsValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("44444444-5555-6666-7777-888888888888");
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
    
    [Fact]
    public async Task Given_PasswordAlreadyInDatabase_When_Validating_Then_FailsValidation()
    {
        // Arrange
        var context = CreateInMemoryDbContext("55555555-6666-7777-8888-999999999999");
        UserManager<User> mockUserManager = CreateFilledUserManager(context);
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        // Try to use the existing password "ExistingP@ssw0rd" which was set in CreateFilledUserManager
        ChangePasswordRequest request = new ChangePasswordRequest(new ChangePasswordDto("ExistingP@ssw0rd", userId));
        ChangePasswordRequestValidator validator = new ChangePasswordRequestValidator(mockUserManager);
        
        // Act
        var resultValidator = await validator.ValidateAsync(request);
        
        // Assert
        Assert.False(resultValidator.IsValid);
        Assert.Contains(resultValidator.Errors, e => e.ErrorMessage == "Please choose a different password.");
    }
}