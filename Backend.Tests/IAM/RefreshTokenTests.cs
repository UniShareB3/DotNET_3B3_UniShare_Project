using Backend.Data;
using Backend.Features.Shared.IAM.RefreshToken;
using Backend.Features.Users.DTO;
using Backend.Persistence;
using Backend.Services.Token;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Backend.Tests.IAM.RefreshToken;

public class RefreshTokenTests
{
    // Static test IDs for reproducibility
    private static readonly Guid TestUserId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestUserId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TestTokenFamily1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TestTokenFamily2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TestTokenId1 = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid TestTokenId2 = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    
    private static Mock<UserManager<User>> CreateMockUserManager(User user)
    {
        var store = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            store.Object, null, null, null, null, null, null, null, null);

        userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
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

    private static Mock<ITokenService> CreateMockTokenService()
    {
        var tokenServiceMock = new Mock<ITokenService>();
        tokenServiceMock.Setup(x => x.GenerateToken(It.IsAny<User>(), It.IsAny<IList<string>>()))
            .Returns("new-access-token");
        tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("new-refresh-token");
        tokenServiceMock.Setup(x => x.GetRefreshTokenExpirationDate())
            .Returns(DateTime.UtcNow.AddDays(7));
        tokenServiceMock.Setup(x => x.GetAccessTokenExpirationInSeconds())
            .Returns(3600);
        return tokenServiceMock;
    }

    private static ApplicationContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new ApplicationContext(options);
        return context;
    }

    private static Data.RefreshToken CreateRefreshToken(Guid userId, Guid tokenFamily, Guid tokenId, bool isExpired = false, bool isRevoked = false)
    {
        return new Data.RefreshToken
        {
            Id = tokenId,
            Token = "test-refresh-token-" + tokenId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = isExpired ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow.AddDays(7),
            IsRevoked = isRevoked,
            RevokedAt = isRevoked ? DateTime.UtcNow : null,
            ReasonRevoked = isRevoked ? "Test revocation" : null,
            TokenFamily = tokenFamily,
            ParentTokenId = null,
            ReplacedByTokenId = null
        };
    }

    [Fact]
    public async Task Given_ValidRefreshToken_When_RefreshToken_Then_ReturnsNewToken()
    {
        // Arrange
        var context = CreateInMemoryDbContext($"RefreshToken_{Guid.NewGuid()}");
        var user = new User
        {
            Id = TestUserId1,
            UserName = "testuser",
            Email = "testuser@uaic.ro"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var refreshToken = CreateRefreshToken(TestUserId1, TestTokenFamily1, TestTokenId1);
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        var userManagerMock = CreateMockUserManager(user);
        var tokenServiceMock = CreateMockTokenService();
        
        var request = new RefreshTokenRequest(refreshToken.Token);
        var handler = new RefreshTokenHandler(userManagerMock.Object, tokenServiceMock.Object, context);

        // Act
        var result = await handler.Handle(request, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        // Verify old token was revoked
        var oldToken = await context.RefreshTokens.FindAsync(refreshToken.Id);
        oldToken.Should().NotBeNull();
        oldToken!.IsRevoked.Should().BeTrue();
        oldToken.ReasonRevoked.Should().Be("Rotated to new token");
        
        // Verify new token was created
        var newToken = await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.ParentTokenId == refreshToken.Id);
        newToken.Should().NotBeNull();
        newToken!.TokenFamily.Should().Be(refreshToken.TokenFamily);
        
        // Cleanup
        await context.DisposeAsync();
    }

    [Fact]
    public async Task Given_ExpiredRefreshToken_When_RefreshToken_Then_ReturnsUnauthorized()
    {
        // Arrange
        var context = CreateInMemoryDbContext($"RefreshToken_{Guid.NewGuid()}");
        var user = new User
        {
            Id = TestUserId1,
            UserName = "testuser",
            Email = "testuser@uaic.ro"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var expiredRefreshToken = CreateRefreshToken(TestUserId1, TestTokenFamily1, TestTokenId1, isExpired: true);
        context.RefreshTokens.Add(expiredRefreshToken);
        await context.SaveChangesAsync();

        var userManagerMock = CreateMockUserManager(user);
        var tokenServiceMock = CreateMockTokenService();
        
        var request = new RefreshTokenRequest(expiredRefreshToken.Token);
        var handler = new RefreshTokenHandler(userManagerMock.Object, tokenServiceMock.Object, context);

        // Act
        var result = await handler.Handle(request, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        
        // Verify no new token was created
        var tokenCount = await context.RefreshTokens.CountAsync();
        tokenCount.Should().Be(1); // Only the original expired token
        
        // Cleanup
        await context.DisposeAsync();
    }

    [Fact]
    public async Task Given_RevokedRefreshToken_When_RefreshToken_Then_ReturnsUnauthorizedAndRevokesFamily()
    {
        // Arrange
        var context = CreateInMemoryDbContext($"RefreshToken_{Guid.NewGuid()}");
        var user = new User
        {
            Id = TestUserId1,
            UserName = "testuser",
            Email = "testuser@uaic.ro"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        // Create a revoked token and an active token in the same family
        var revokedToken = new Data.RefreshToken
        {
            Id = TestTokenId1,
            Token = "revoked-token",
            UserId = TestUserId1,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ExpiresAt = DateTime.UtcNow.AddDays(5),
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow.AddDays(-1),
            ReasonRevoked = "Already revoked",
            TokenFamily = TestTokenFamily1,
            ParentTokenId = null,
            ReplacedByTokenId = null
        };
        
        var activeTokenInFamily = new Data.RefreshToken
        {
            Id = TestTokenId2,
            Token = "active-token-in-family",
            UserId = TestUserId1,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            TokenFamily = TestTokenFamily1,
            ParentTokenId = TestTokenId1,
            ReplacedByTokenId = null
        };
        
        context.RefreshTokens.AddRange(revokedToken, activeTokenInFamily);
        await context.SaveChangesAsync();

        var userManagerMock = CreateMockUserManager(user);
        var tokenServiceMock = CreateMockTokenService();
        
        var request = new RefreshTokenRequest(revokedToken.Token);
        var handler = new RefreshTokenHandler(userManagerMock.Object, tokenServiceMock.Object, context);

        // Act
        var result = await handler.Handle(request, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        
        // Verify entire token family was revoked
        var allTokens = await context.RefreshTokens
            .Where(rt => rt.TokenFamily == TestTokenFamily1)
            .ToListAsync();
        allTokens.Should().HaveCount(2);
        allTokens.Should().OnlyContain(token => token.IsRevoked == true);
        
        // Verify that the active token now has the reuse detection reason
        var updatedActiveToken = allTokens.First(t => t.Id == TestTokenId2);
        updatedActiveToken.ReasonRevoked.Should().Contain("Token reuse detected");
        
        // Cleanup
        await context.DisposeAsync();
    }

    [Fact]
    public async Task Given_UserNotFound_When_RefreshToken_Then_ReturnsUnauthorized()
    {
        // Arrange
        var context = CreateInMemoryDbContext($"RefreshToken_{Guid.NewGuid()}");

        var refreshToken = CreateRefreshToken(TestUserId1, TestTokenFamily1, TestTokenId1);
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        var userManagerMock = CreateMockUserManagerNotFound();
        var tokenServiceMock = CreateMockTokenService();
        
        var request = new RefreshTokenRequest(refreshToken.Token);
        var handler = new RefreshTokenHandler(userManagerMock.Object, tokenServiceMock.Object, context);

        // Act
        var result = await handler.Handle(request, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        
        // Verify no new token was created
        var tokenCount = await context.RefreshTokens.CountAsync();
        tokenCount.Should().Be(1); // Only the original token
        
        // Cleanup
        await context.DisposeAsync();
    }

    [Fact]
    public async Task Given_TokenNotInDatabase_When_RefreshToken_Then_ReturnsUnauthorized()
    {
        // Arrange
        var context = CreateInMemoryDbContext($"RefreshToken_{Guid.NewGuid()}");
        var user = new User
        {
            Id = TestUserId1,
            UserName = "testuser",
            Email = "testuser@uaic.ro"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var userManagerMock = CreateMockUserManager(user);
        var tokenServiceMock = CreateMockTokenService();
        
        var request = new RefreshTokenRequest("non-existent-token");
        var handler = new RefreshTokenHandler(userManagerMock.Object, tokenServiceMock.Object, context);

        // Act
        var result = await handler.Handle(request, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        
        // Cleanup
        await context.DisposeAsync();
    }
}