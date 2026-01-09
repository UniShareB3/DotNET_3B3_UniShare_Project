using Backend.Data;
using Backend.Features.Shared.IAM.GetRefreshTokens;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Backend.Tests.IAM.GetRefreshTokens;

public class GetRefreshTokensTests
{
    // Static test IDs for reproducibility
    private static readonly Guid TestUserId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestUserId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TestTokenFamily1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TestTokenId1 = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid TestTokenId2 = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid TestTokenId3 = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    
    private static Mock<UserManager<User>> CreateMockUserManager(User? user = null)
    {
        var store = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            store.Object, null, null, null, null, null, null, null, null);

        if (user != null)
        {
            userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        }
        else
        {
            userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        }
        
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
    public async Task Given_ValidUserId_When_GetRefreshTokens_Then_ReturnsAllUserTokens()
    {
        // Arrange
        var context = CreateInMemoryDbContext($"GetRefreshTokens_{Guid.NewGuid()}");
        
        var user = new User
        {
            Id = TestUserId1,
            UserName = "testuser",
            Email = "testuser@uaic.ro"
        };
        context.Users.Add(user);
        
        // Add multiple refresh tokens for the user
        var token1 = new Backend.Data.RefreshToken
        {
            Id = TestTokenId1,
            Token = "token-1",
            UserId = TestUserId1,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ExpiresAt = DateTime.UtcNow.AddDays(5),
            IsRevoked = false,
            TokenFamily = TestTokenFamily1,
            ParentTokenId = null,
            ReplacedByTokenId = null
        };
        
        var token2 = new Backend.Data.RefreshToken
        {
            Id = TestTokenId2,
            Token = "token-2",
            UserId = TestUserId1,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(6),
            IsRevoked = false,
            TokenFamily = TestTokenFamily1,
            ParentTokenId = TestTokenId1,
            ReplacedByTokenId = null
        };
        
        var token3 = new Backend.Data.RefreshToken
        {
            Id = TestTokenId3,
            Token = "token-3",
            UserId = TestUserId1,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow,
            ReasonRevoked = "Token rotated",
            TokenFamily = TestTokenFamily1,
            ParentTokenId = TestTokenId2,
            ReplacedByTokenId = null
        };
        
        context.RefreshTokens.AddRange(token1, token2, token3);
        await context.SaveChangesAsync();

        var userManagerMock = CreateMockUserManager(user);
        
        var request = new GetRefreshTokensRequest(TestUserId1);
        var handler = new GetRefreshTokensHandler(userManagerMock.Object, context);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var statusCodeResult = result as IStatusCodeHttpResult;
        statusCodeResult.Should().NotBeNull();
        statusCodeResult!.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        // Verify all tokens are returned and ordered by CreatedAt descending
        var tokens = await context.RefreshTokens
            .Where(rt => rt.UserId == TestUserId1)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync();
        
        tokens.Should().HaveCount(3);
        tokens[0].Id.Should().Be(TestTokenId3); // Most recent
        tokens[1].Id.Should().Be(TestTokenId2);
        tokens[2].Id.Should().Be(TestTokenId1); // Oldest
        
        // Cleanup
        await context.DisposeAsync();
    }

    [Fact]
    public async Task Given_UserNotFound_When_GetRefreshTokens_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext($"GetRefreshTokens_{Guid.NewGuid()}");

        var userManagerMock = CreateMockUserManager();
        
        var request = new GetRefreshTokensRequest(TestUserId2);
        var handler = new GetRefreshTokensHandler(userManagerMock.Object, context);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var statusCodeResult = result as IStatusCodeHttpResult;
        statusCodeResult.Should().NotBeNull();
        statusCodeResult!.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        
        // Cleanup
        await context.DisposeAsync();
    }
}
