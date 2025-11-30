using Moq;
using FluentAssertions;
using Backend.Features.Users;
using Backend.Data;
using Backend.Services;
using Backend.TokenGenerators; 
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Backend.Persistence;
using Microsoft.AspNetCore.Http;

public class ConfirmEmailHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IHashingService> _hashingServiceMock;
    private readonly ApplicationContext context;
    private readonly TokenService _tokenService; // We use the REAL service

    public ConfirmEmailHandlerTests()
    {
        _userManagerMock = CreateUserManagerMock();
        _hashingServiceMock = new Mock<IHashingService>();
        context = CreateInMemoryDbContext(Guid.NewGuid().ToString());

        var inMemorySettings = new Dictionary<string, string> {
            {"JwtSettings:Key", "ThisIsASecretKeyForTestingPurposesOnly123!"}, // Must be long enough for HMACSHA256
            {"JwtSettings:Issuer", "TestIssuer"},
            {"JwtSettings:Audience", "TestAudience"},
            {"JwtSettings:ExpiryTime", "900"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        _tokenService = new TokenService(configuration);
    }

    [Fact]
    public async Task Handle_GivenInvalidJwtToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new ConfirmEmailRequest("invalid-token-string", "123456");
        var handler = new ConfirmEmailHandler(_userManagerMock.Object, context, _hashingServiceMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;

        GetProperty(valueResult.Value, "error").Should().Be("Invalid JWT token");
    }

    [Fact]
    public async Task Handle_GivenValidTokenButUserNotFound_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        var userForToken = new User { Id = userId, Email = "test@example.com" };
        var token = _tokenService.GenerateToken(userForToken);
        
        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((User)null);

        var request = new ConfirmEmailRequest(token, "123456");
        var handler = new ConfirmEmailHandler(_userManagerMock.Object, context, _hashingServiceMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;

        GetProperty(valueResult.Value, "error").Should().Be("User not found");
    }

    [Fact]
    public async Task Handle_GivenValidRequest_UpdatesUserAndToken_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", EmailConfirmed = false };
        
        var jwtToken = _tokenService.GenerateToken(user);
        
        var inputCode = "123456";
        var expectedHash = "hashed_123456";

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        _hashingServiceMock.Setup(x => x.HashCode(inputCode)).Returns(expectedHash);

        var dbToken = new EmailConfirmationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Code = expectedHash,
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        };
        context.EmailConfirmationTokens.Add(dbToken);
        await context.SaveChangesAsync();

        var request = new ConfirmEmailRequest(jwtToken, inputCode);
        var handler = new ConfirmEmailHandler(_userManagerMock.Object, context, _hashingServiceMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        badRequest.StatusCode.Should().Be(StatusCodes.Status200OK);

        user.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GivenValidUserButWrongCode_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", EmailConfirmed = false };
        var token = _tokenService.GenerateToken(user);
        
        var inputCode = "123456";
        var wrongHash = "wrong_hash";

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _hashingServiceMock.Setup(x => x.HashCode(inputCode)).Returns(wrongHash);

        var dbToken = new EmailConfirmationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Code = "correct_hash",
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        };
        context.EmailConfirmationTokens.Add(dbToken);
        await context.SaveChangesAsync();

        var request = new ConfirmEmailRequest(token, inputCode);
        var handler = new ConfirmEmailHandler(_userManagerMock.Object, context, _hashingServiceMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;

        GetProperty(valueResult.Value, "error").Should().Be("Invalid or expired verification code");
    }

    private object GetProperty(object obj, string propertyName)
    {
        return obj.GetType().GetProperty(propertyName)?.GetValue(obj, null);
    }

    private ApplicationContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationContext(options);
    }

    private Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
    }
}