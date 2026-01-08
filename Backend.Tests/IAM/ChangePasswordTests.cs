using Backend.Data;
using Backend.Features.Shared.IAM.ChangePassword;
using Backend.Features.Shared.IAM.DTO;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Backend.Tests.IAM.ChangePassword;

public class ChangePasswordTests
{
    // Static test IDs for reproducibility
    private static readonly Guid TestUserId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestUserId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TestNonExistentUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TestTokenId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TestTokenId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TestTokenId3 = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    
    private static Mock<UserManager<User>> CreateMockUserManager(User user)
    {
        var store = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            store.Object, null, null, null, null, null, null, null, null);

        userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        return userManagerMock;
    }

    private static Mock<UserManager<User>> CreateMockUserManagerNotFound()
    {
        var store = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            store.Object, null, null, null, null, null, null, null, null);

        userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((User)null);
        return userManagerMock;
    }

    private static ApplicationContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new ApplicationContext(options);
        return context;
    }

    [Fact]
    public async Task Given_ValidUserIdAndToken_When_ChangePassword_Then_ReturnsSuccess()
    {
        // Arrange
        var context = CreateInMemoryDbContext($"ChangePassword_{Guid.NewGuid()}");
        
        User testUser = new User
        {
            Id = TestUserId1,
            UserName = "testuser",
            Email = "testuser@uaic.ro"
        };
        
        // Add a valid password reset token
        var resetToken = new PasswordResetToken
        {
            Id = TestTokenId1,
            UserId = testUser.Id,
            Code = "valid-reset-token",
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        };
        context.PasswordResetTokens.Add(resetToken);
        await context.SaveChangesAsync();

        var userManagerMock = CreateMockUserManager(testUser);
        userManagerMock.Setup(um => um.ResetPasswordAsync(
                It.Is<User>(u => u.Id == testUser.Id),
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        
        var userManager = userManagerMock.Object;
        var changePasswordDto = new ChangePasswordDto("NewPassword123!", testUser.Id);
        var changePasswordRequest = new ChangePasswordRequest(changePasswordDto);
        var changePasswordHandler = new ChangePasswordHandler(userManager, context);

        // Act
        var result = await changePasswordHandler.Handle(changePasswordRequest, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        // Verify token was marked as used and removed
        var usedToken = await context.PasswordResetTokens.FindAsync(resetToken.Id);
        usedToken.Should().BeNull(); // Token should be removed after use
        
        // Cleanup
        await context.DisposeAsync();
    }

    [Fact]
    public async Task Given_ExpiredToken_When_ChangePassword_Then_ReturnsBadRequest()
    {
        // Arrange
        var context = CreateInMemoryDbContext($"ChangePassword_{Guid.NewGuid()}");
        
        User testUser = new User
        {
            Id = TestUserId1,
            UserName = "testuser",
            Email = "testuser@uaic.ro"
        };
        
        // Add an expired password reset token
        var expiredToken = new PasswordResetToken
        {
            Id = TestTokenId1,
            UserId = testUser.Id,
            Code = "expired-token",
            CreatedAt = DateTime.UtcNow.AddMinutes(-20), // Expired (more than 15 minutes)
            IsUsed = false
        };
        context.PasswordResetTokens.Add(expiredToken);
        await context.SaveChangesAsync();

        var userManagerMock = CreateMockUserManager(testUser);
        var userManager = userManagerMock.Object;
        var changePasswordDto = new ChangePasswordDto("NewPassword123!", testUser.Id);
        var changePasswordRequest = new ChangePasswordRequest(changePasswordDto);
        var changePasswordHandler = new ChangePasswordHandler(userManager, context);

        // Act
        var result = await changePasswordHandler.Handle(changePasswordRequest, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        
        // Cleanup
        await context.DisposeAsync();
    }

    [Fact]
    public async Task Given_NoToken_When_ChangePassword_Then_ReturnsBadRequest()
    {
        // Arrange
        var context = CreateInMemoryDbContext($"ChangePassword_{Guid.NewGuid()}");
        
        User testUser = new User
        {
            Id = TestUserId1,
            UserName = "testuser",
            Email = "testuser@uaic.ro"
        };

        var userManagerMock = CreateMockUserManager(testUser);
        var userManager = userManagerMock.Object;
        var changePasswordDto = new ChangePasswordDto("NewPassword123!", testUser.Id);
        var changePasswordRequest = new ChangePasswordRequest(changePasswordDto);
        var changePasswordHandler = new ChangePasswordHandler(userManager, context);

        // Act
        var result = await changePasswordHandler.Handle(changePasswordRequest, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        
        // Cleanup
        await context.DisposeAsync();
    }

    [Fact]
    public async Task Given_UserNotFound_When_ChangePassword_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext($"ChangePassword_{Guid.NewGuid()}");
        
        var userManagerMock = CreateMockUserManagerNotFound();
        var userManager = userManagerMock.Object;
        var changePasswordDto = new ChangePasswordDto("NewPassword123!", TestNonExistentUserId);
        var changePasswordRequest = new ChangePasswordRequest(changePasswordDto);
        var changePasswordHandler = new ChangePasswordHandler(userManager, context);

        // Act
        var result = await changePasswordHandler.Handle(changePasswordRequest, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        
        // Cleanup
        await context.DisposeAsync();
    }
    
    [Fact]
    public async Task Given_ResetPasswordFails_When_ChangePassword_Then_ReturnsBadRequest()
    {
        // Arrange
        var context = CreateInMemoryDbContext($"ChangePassword_{Guid.NewGuid()}");
        
        User testUser = new User
        {
            Id = TestUserId2,
            UserName = "testuser",
            Email = "testuser@uaic.ro"
        };
        
        // Add a valid password reset token
        var resetToken = new PasswordResetToken
        {
            Id = TestTokenId2,
            UserId = testUser.Id,
            Code = "valid-reset-token",
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        };
        context.PasswordResetTokens.Add(resetToken);
        await context.SaveChangesAsync();

        var userManagerMock = CreateMockUserManager(testUser);
        
        // Mock ResetPasswordAsync to return failure with errors
        var identityErrors = new[]
        {
            new IdentityError { Code = "PasswordTooShort", Description = "Passwords must be at least 6 characters." },
            new IdentityError { Code = "PasswordRequiresDigit", Description = "Passwords must have at least one digit ('0'-'9')." }
        };
        var failedResult = IdentityResult.Failed(identityErrors);
        
        userManagerMock.Setup(um => um.ResetPasswordAsync(
                It.Is<User>(u => u.Id == testUser.Id),
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .ReturnsAsync(failedResult);
        
        var userManager = userManagerMock.Object;
        var changePasswordDto = new ChangePasswordDto("weak", testUser.Id);
        var changePasswordRequest = new ChangePasswordRequest(changePasswordDto);
        var changePasswordHandler = new ChangePasswordHandler(userManager, context);

        // Act
        var result = await changePasswordHandler.Handle(changePasswordRequest, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        
        // Verify token was marked as used but not removed (since password reset failed)
        var tokenStillExists = await context.PasswordResetTokens.FindAsync(resetToken.Id);
        tokenStillExists.Should().NotBeNull();
        tokenStillExists!.IsUsed.Should().BeTrue();
        
        // Cleanup
        await context.DisposeAsync();
    }
}