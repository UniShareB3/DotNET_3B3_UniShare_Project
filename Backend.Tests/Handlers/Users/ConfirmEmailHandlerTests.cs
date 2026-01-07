using Moq;
using FluentAssertions;
using Backend.Data;
using Backend.Features.Users.ConfirmEmail;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Backend.Persistence;
using Backend.Services.Hashing;
using Microsoft.AspNetCore.Http;

namespace Backend.Tests.Handlers.Users;

public class ConfirmEmailHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock = CreateUserManagerMock();
    private readonly Mock<IHashingService> _hashingServiceMock = new();
    private readonly ApplicationContext _context = CreateInMemoryDbContext(Guid.NewGuid().ToString());

    [Fact]
    public async Task GivenUserNotFound_WhenConfirmingEmail_ThenReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(default(User?));

        var request = new ConfirmEmailRequest(userId, "123456");
        var handler = new ConfirmEmailHandler(_userManagerMock.Object, _context, _hashingServiceMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        valueResult.Value.Should().NotBeNull();
        GetProperty(valueResult.Value, "error").Should().Be("User not found");
    }

    [Fact]
    public async Task GivenEmailAlreadyConfirmed_WhenConfirmingEmail_ThenReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", NewEmailConfirmed = true };
        
        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

        var request = new ConfirmEmailRequest(userId, "123456");
        var handler = new ConfirmEmailHandler(_userManagerMock.Object, _context, _hashingServiceMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        valueResult.Value.Should().NotBeNull();
        GetProperty(valueResult.Value, "error").Should().Be("Email already confirmed");
    }

    [Fact]
    public async Task GivenValidRequest_WhenConfirmingEmail_ThenUpdatesUserAndToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", NewEmailConfirmed = false };
        
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
        _context.EmailConfirmationTokens.Add(dbToken);
        await _context.SaveChangesAsync();

        var request = new ConfirmEmailRequest(userId, inputCode);
        var handler = new ConfirmEmailHandler(_userManagerMock.Object, _context, _hashingServiceMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        user.NewEmailConfirmed.Should().BeTrue();
        
        // Verify the token was marked as used
        var updatedToken = await _context.EmailConfirmationTokens.FindAsync(dbToken.Id);
        updatedToken.Should().NotBeNull();
        updatedToken.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task GivenValidUserButWrongCode_WhenConfirmingEmail_ThenReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", NewEmailConfirmed = false };
        
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
        _context.EmailConfirmationTokens.Add(dbToken);
        await _context.SaveChangesAsync();

        var request = new ConfirmEmailRequest(userId, inputCode);
        var handler = new ConfirmEmailHandler(_userManagerMock.Object, _context, _hashingServiceMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        valueResult.Value.Should().NotBeNull();
        GetProperty(valueResult.Value, "error").Should().Be("Invalid or expired verification code");
    }

    [Fact]
    public async Task GivenExpiredToken_WhenConfirmingEmail_ThenReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", NewEmailConfirmed = false };
        
        var inputCode = "123456";
        var expectedHash = "hashed_123456";

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _hashingServiceMock.Setup(x => x.HashCode(inputCode)).Returns(expectedHash);

        var dbToken = new EmailConfirmationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Code = expectedHash,
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10), // Expired 10 minutes ago
            CreatedAt = DateTime.UtcNow.AddMinutes(-15)
        };
        _context.EmailConfirmationTokens.Add(dbToken);
        await _context.SaveChangesAsync();

        var request = new ConfirmEmailRequest(userId, inputCode);
        var handler = new ConfirmEmailHandler(_userManagerMock.Object, _context, _hashingServiceMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        valueResult.Value.Should().NotBeNull();
        GetProperty(valueResult.Value, "error").Should().Be("Invalid or expired verification code");
    }

    [Fact]
    public async Task GivenAlreadyUsedToken_WhenConfirmingEmail_ThenReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", NewEmailConfirmed = false };
        
        var inputCode = "123456";
        var expectedHash = "hashed_123456";

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _hashingServiceMock.Setup(x => x.HashCode(inputCode)).Returns(expectedHash);

        var dbToken = new EmailConfirmationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Code = expectedHash,
            IsUsed = true, // Already used
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        };
        _context.EmailConfirmationTokens.Add(dbToken);
        await _context.SaveChangesAsync();

        var request = new ConfirmEmailRequest(userId, inputCode);
        var handler = new ConfirmEmailHandler(_userManagerMock.Object, _context, _hashingServiceMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        valueResult.Value.Should().NotBeNull();
        GetProperty(valueResult.Value, "error").Should().Be("Invalid or expired verification code");
    }

    [Fact]
    public async Task GivenMultipleTokens_WhenConfirmingEmail_ThenUsesTheMostRecentValidOne()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", NewEmailConfirmed = false };
        
        var inputCode = "123456";
        var expectedHash = "hashed_123456";

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _hashingServiceMock.Setup(x => x.HashCode(inputCode)).Returns(expectedHash);

        // Add older token
        var olderToken = new EmailConfirmationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Code = expectedHash,
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        _context.EmailConfirmationTokens.Add(olderToken);

        // Add newer token
        var newerToken = new EmailConfirmationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Code = expectedHash,
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        };
        _context.EmailConfirmationTokens.Add(newerToken);
        await _context.SaveChangesAsync();

        var request = new ConfirmEmailRequest(userId, inputCode);
        var handler = new ConfirmEmailHandler(_userManagerMock.Object, _context, _hashingServiceMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        // Verify the newer token was marked as used
        var updatedNewerToken = await _context.EmailConfirmationTokens.FindAsync(newerToken.Id);
        updatedNewerToken.Should().NotBeNull();
        updatedNewerToken.IsUsed.Should().BeTrue();

        // The older token should remain unused
        var updatedOlderToken = await _context.EmailConfirmationTokens.FindAsync(olderToken.Id);
        updatedOlderToken.Should().NotBeNull();
        updatedOlderToken.IsUsed.Should().BeFalse();
    }

    private static object? GetProperty(object obj, string propertyName)
    {
        return obj.GetType().GetProperty(propertyName)?.GetValue(obj, null);
    }

    private static ApplicationContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationContext(options);
    }

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        var optionsMock = new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>();
        var passwordHasherMock = new Mock<IPasswordHasher<User>>();
        var userValidators = new List<IUserValidator<User>>();
        var passwordValidators = new List<IPasswordValidator<User>>();
        var keyNormalizer = new Mock<ILookupNormalizer>();
        var errors = new Mock<IdentityErrorDescriber>();
        var servicesMock = new Mock<IServiceProvider>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<UserManager<User>>>();

        return new Mock<UserManager<User>>(
            store.Object,
            optionsMock.Object,
            passwordHasherMock.Object,
            userValidators,
            passwordValidators,
            keyNormalizer.Object,
            errors.Object,
            servicesMock.Object,
            loggerMock.Object);
    }
}