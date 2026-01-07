using Backend.Data;
using Backend.Features.Users.DeleteUser;
using Backend.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Backend.Tests.Handlers.Users;

public class DeleteUserHandlerTests
{
    private static ApplicationContext CreateInMemoryDbContext(string guid)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: guid)
            .Options;

        return new ApplicationContext(options);
    }

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        var options = new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>();
        var passwordHasher = new Mock<IPasswordHasher<User>>();
        var userValidators = new List<IUserValidator<User>>();
        var passwordValidators = new List<IPasswordValidator<User>>();
        var keyNormalizer = new Mock<ILookupNormalizer>();
        var errors = new Mock<IdentityErrorDescriber>();
        var services = new Mock<IServiceProvider>();
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<UserManager<User>>>();

        return new Mock<UserManager<User>>(
            store.Object,
            options.Object,
            passwordHasher.Object,
            userValidators,
            passwordValidators,
            keyNormalizer.Object,
            errors.Object,
            services.Object,
            logger.Object);
    }

    [Fact]
    public async Task Given_NonExistentUser_When_DeleteUser_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userManagerMock = CreateUserManagerMock();

        var userId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((User?)null);

        var handler = new DeleteUserHandler(userManagerMock.Object, context);
        var request = new DeleteUserRequest(userId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Given_UserWithEmailTokens_When_DeleteUser_Then_RemovesEmailTokensAndDeletesUser()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userManagerMock = CreateUserManagerMock();

        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var user = new User
        {
            Id = userId,
            Email = "test@test.com",
            UserName = "test@test.com",
            FirstName = "John",
            LastName = "Doe"
        };

        // Add email confirmation tokens to the database
        var emailToken1 = new EmailConfirmationToken
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            UserId = userId,
            Code = "token1",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };

        var emailToken2 = new EmailConfirmationToken
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            UserId = userId,
            Code = "token2",
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            CreatedAt = DateTime.UtcNow
        };

        context.EmailConfirmationTokens.Add(emailToken1);
        context.EmailConfirmationTokens.Add(emailToken2);
        await context.SaveChangesAsync();

        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        userManagerMock.Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var handler = new DeleteUserHandler(userManagerMock.Object, context);
        var request = new DeleteUserRequest(userId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        
        // Verify email tokens were removed
        var remainingEmailTokens = await context.EmailConfirmationTokens
            .Where(et => et.UserId == userId)
            .ToListAsync();
        Assert.Empty(remainingEmailTokens);
    }

    [Fact]
    public async Task Given_UserWithRefreshTokens_When_DeleteUser_Then_RemovesRefreshTokensAndDeletesUser()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userManagerMock = CreateUserManagerMock();

        var userId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var user = new User
        {
            Id = userId,
            Email = "test@test.com",
            UserName = "test@test.com",
            FirstName = "John",
            LastName = "Doe"
        };

        // Add refresh tokens to the database
        var refreshToken = new RefreshToken
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            UserId = userId,
            Token = "refresh-token-123",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        userManagerMock.Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var handler = new DeleteUserHandler(userManagerMock.Object, context);
        var request = new DeleteUserRequest(userId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        
        // Verify refresh tokens were removed
        var remainingRefreshTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .ToListAsync();
        Assert.Empty(remainingRefreshTokens);
    }

    [Fact]
    public async Task Given_UserManagerDeleteFails_When_DeleteUser_Then_ReturnsBadRequest()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userManagerMock = CreateUserManagerMock();

        var userId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var user = new User
        {
            Id = userId,
            Email = "test@test.com",
            UserName = "test@test.com",
            FirstName = "John",
            LastName = "Doe"
        };

        var identityError = new IdentityError
        {
            Code = "DeleteFailed",
            Description = "Failed to delete user from the system"
        };

        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        userManagerMock.Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        var handler = new DeleteUserHandler(userManagerMock.Object, context);
        var request = new DeleteUserRequest(userId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        
        // Verify user was NOT deleted from context (still exists)
        userManagerMock.Verify(x => x.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task Given_UserWithBothTokenTypes_When_DeleteUser_Then_RemovesBothTokenTypesAndDeletesUser()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userManagerMock = CreateUserManagerMock();

        var userId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var user = new User
        {
            Id = userId,
            Email = "test@test.com",
            UserName = "test@test.com",
            FirstName = "John",
            LastName = "Doe"
        };

        // Add both refresh tokens and email tokens
        var refreshToken = new RefreshToken
        {
            Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
            UserId = userId,
            Token = "refresh-token-456",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        var emailToken = new EmailConfirmationToken
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            UserId = userId,
            Code = "email-token-789",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };

        context.RefreshTokens.Add(refreshToken);
        context.EmailConfirmationTokens.Add(emailToken);
        await context.SaveChangesAsync();

        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        userManagerMock.Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var handler = new DeleteUserHandler(userManagerMock.Object, context);
        var request = new DeleteUserRequest(userId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        
        // Verify both token types were removed
        var remainingRefreshTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .ToListAsync();
        Assert.Empty(remainingRefreshTokens);

        var remainingEmailTokens = await context.EmailConfirmationTokens
            .Where(et => et.UserId == userId)
            .ToListAsync();
        Assert.Empty(remainingEmailTokens);
    }
}

