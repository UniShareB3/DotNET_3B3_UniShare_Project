using Backend.Data;
using Backend.Features.Shared.IAM.RequestPasswordReset;
using Backend.Persistence;
using Backend.Services.EmailSender;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Backend.Tests.IAM.RequestPasswordReset;

public class RequestPasswordResetTests
{
    // Static test IDs for reproducibility
    private static readonly Guid TestUserId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestTokenId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TestTokenId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    
    private static Mock<UserManager<User>> CreateMockUserManager(User? user = null)
    {
        var store = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            store.Object, null, null, null, null, null, null, null, null);

        if (user != null)
        {
            userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync("generated-reset-token-" + user.Id);
        }
        else
        {
            userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        }
        
        return userManagerMock;
    }

    private static Mock<IEmailSender> CreateMockEmailSender(bool shouldFail = false)
    {
        var emailSenderMock = new Mock<IEmailSender>();
        
        if (shouldFail)
        {
            emailSenderMock.Setup(x => x.SendPasswordResetEmailAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("Email service unavailable"));
        }
        else
        {
            emailSenderMock.Setup(x => x.SendPasswordResetEmailAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);
        }
        
        return emailSenderMock;
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
    public async Task Given_ValidEmail_When_RequestPasswordReset_Then_ReturnsSuccessAndSendsEmail()
    {
        // Arrange
        var context = CreateInMemoryDbContext($"RequestPasswordReset_{Guid.NewGuid()}");
        
        var user = new User
        {
            Id = TestUserId1,
            UserName = "testuser",
            Email = "testuser@uaic.ro"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var userManagerMock = CreateMockUserManager(user);
        var emailSenderMock = CreateMockEmailSender();
        
        var request = new RequestPasswordResetRequest("testuser@uaic.ro");
        var handler = new RequestPasswordResetHandler(userManagerMock.Object, context, emailSenderMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        // Verify token was created in database
        var tokens = await context.PasswordResetTokens.Where(t => t.UserId == user.Id).ToListAsync();
        tokens.Should().HaveCount(1);
        tokens[0].Code.Should().Be("generated-reset-token-" + user.Id);
        tokens[0].IsUsed.Should().BeFalse();
        tokens[0].ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        
        // Verify email was sent
        emailSenderMock.Verify(x => x.SendPasswordResetEmailAsync(
            user.Email, 
            "generated-reset-token-" + user.Id, 
            user.Id), Times.Once);
        
        // Cleanup
        await context.DisposeAsync();
    }

    [Fact]
    public async Task Given_UserNotFound_When_RequestPasswordReset_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext($"RequestPasswordReset_{Guid.NewGuid()}");

        var userManagerMock = CreateMockUserManager(null);
        var emailSenderMock = CreateMockEmailSender();
        
        var request = new RequestPasswordResetRequest("nonexistent@uaic.ro");
        var handler = new RequestPasswordResetHandler(userManagerMock.Object, context, emailSenderMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        
        // Verify no token was created
        var tokens = await context.PasswordResetTokens.ToListAsync();
        tokens.Should().BeEmpty();
        
        // Verify no email was sent
        emailSenderMock.Verify(x => x.SendPasswordResetEmailAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<Guid>()), Times.Never);
        
        // Cleanup
        await context.DisposeAsync();
    }

    [Fact]
    public async Task Given_UserWithNoEmail_When_RequestPasswordReset_Then_ReturnsBadRequest()
    {
        // Arrange
        var context = CreateInMemoryDbContext($"RequestPasswordReset_{Guid.NewGuid()}");
        
        var user = new User
        {
            Id = TestUserId1,
            UserName = "testuser",
            Email = null // User has no email
        };

        var store = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            store.Object, null, null, null, null, null, null, null, null);
        
        userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        
        var emailSenderMock = CreateMockEmailSender();
        
        var request = new RequestPasswordResetRequest("test@uaic.ro");
        var handler = new RequestPasswordResetHandler(userManagerMock.Object, context, emailSenderMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        
        // Verify no token was created
        var tokens = await context.PasswordResetTokens.ToListAsync();
        tokens.Should().BeEmpty();
        
        // Verify no email was sent
        emailSenderMock.Verify(x => x.SendPasswordResetEmailAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<Guid>()), Times.Never);
        
        // Cleanup
        await context.DisposeAsync();
    }

    [Fact]
    public async Task Given_ExistingUnusedTokens_When_RequestPasswordReset_Then_RemovesOldTokensAndCreatesNew()
    {
        // Arrange
        var context = CreateInMemoryDbContext($"RequestPasswordReset_{Guid.NewGuid()}");
        
        var user = new User
        {
            Id = TestUserId1,
            UserName = "testuser",
            Email = "testuser@uaic.ro"
        };
        context.Users.Add(user);
        
        // Add existing unused tokens
        var oldToken1 = new PasswordResetToken
        {
            Id = TestTokenId1,
            UserId = user.Id,
            Code = "old-token-1",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false
        };
        
        var oldToken2 = new PasswordResetToken
        {
            Id = TestTokenId2,
            UserId = user.Id,
            Code = "old-token-2",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };
        
        context.PasswordResetTokens.AddRange(oldToken1, oldToken2);
        await context.SaveChangesAsync();

        var userManagerMock = CreateMockUserManager(user);
        var emailSenderMock = CreateMockEmailSender();
        
        var request = new RequestPasswordResetRequest("testuser@uaic.ro");
        var handler = new RequestPasswordResetHandler(userManagerMock.Object, context, emailSenderMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        // Verify old tokens were removed and new token was created
        var tokens = await context.PasswordResetTokens.Where(t => t.UserId == user.Id).ToListAsync();
        tokens.Should().HaveCount(1);
        tokens[0].Code.Should().Be("generated-reset-token-" + user.Id);
        tokens[0].Id.Should().NotBe(TestTokenId1);
        tokens[0].Id.Should().NotBe(TestTokenId2);
        
        // Cleanup
        await context.DisposeAsync();
    }

    [Fact]
    public async Task Given_EmailServiceFails_When_RequestPasswordReset_Then_ReturnsProblem()
    {
        // Arrange
        var context = CreateInMemoryDbContext($"RequestPasswordReset_{Guid.NewGuid()}");
        
        var user = new User
        {
            Id = TestUserId1,
            UserName = "testuser",
            Email = "testuser@uaic.ro"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var userManagerMock = CreateMockUserManager(user);
        var emailSenderMock = CreateMockEmailSender(shouldFail: true);
        
        var request = new RequestPasswordResetRequest("testuser@uaic.ro");
        var handler = new RequestPasswordResetHandler(userManagerMock.Object, context, emailSenderMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        
        // Verify token was still created in database (transaction not rolled back)
        var tokens = await context.PasswordResetTokens.Where(t => t.UserId == user.Id).ToListAsync();
        tokens.Should().HaveCount(1);
        
        // Cleanup
        await context.DisposeAsync();
    }

    [Fact]
    public async Task Given_UsedTokenExists_When_RequestPasswordReset_Then_DoesNotRemoveUsedToken()
    {
        // Arrange
        var context = CreateInMemoryDbContext($"RequestPasswordReset_{Guid.NewGuid()}");
        
        var user = new User
        {
            Id = TestUserId1,
            UserName = "testuser",
            Email = "testuser@uaic.ro"
        };
        context.Users.Add(user);
        
        // Add a used token (should not be removed)
        var usedToken = new PasswordResetToken
        {
            Id = TestTokenId1,
            UserId = user.Id,
            Code = "used-token",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = true
        };
        
        // Add an unused token (should be removed)
        var unusedToken = new PasswordResetToken
        {
            Id = TestTokenId2,
            UserId = user.Id,
            Code = "unused-token",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };
        
        context.PasswordResetTokens.AddRange(usedToken, unusedToken);
        await context.SaveChangesAsync();

        var userManagerMock = CreateMockUserManager(user);
        var emailSenderMock = CreateMockEmailSender();
        
        var request = new RequestPasswordResetRequest("testuser@uaic.ro");
        var handler = new RequestPasswordResetHandler(userManagerMock.Object, context, emailSenderMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        // Verify used token still exists and unused token was removed
        var allTokens = await context.PasswordResetTokens.Where(t => t.UserId == user.Id).ToListAsync();
        allTokens.Should().HaveCount(2); // Used token + new token
        allTokens.Should().Contain(t => t.Id == TestTokenId1 && t.IsUsed);
        allTokens.Should().Contain(t => t.Code == "generated-reset-token-" + user.Id && !t.IsUsed);
        
        // Cleanup
        await context.DisposeAsync();
    }
}

