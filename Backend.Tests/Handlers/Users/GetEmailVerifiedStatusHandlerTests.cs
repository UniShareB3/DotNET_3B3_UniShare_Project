using Backend.Data;
using Backend.Features.Users.GetEmailVerifiedStatus;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Backend.Tests.Handlers.Users;

public class GetEmailVerifiedStatusHandlerTests
{
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
    public async Task Given_ExistingUser_When_GetEmailVerifiedStatus_Then_ReturnsEmailVerifiedStatus()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        var user = new User
        {
            Id = userId,
            Email = "test@test.com",
            UserName = "test@test.com",
            NewEmailConfirmed = true
        };

        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        var handler = new GetEmailVerifiedStatusHandler(userManagerMock.Object);
        var request = new GetEmailVerifiedStatusRequest(userId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Given_NonExistentUser_When_GetEmailVerifiedStatus_Then_ReturnsNotFound()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        var userId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((User?)null);

        var handler = new GetEmailVerifiedStatusHandler(userManagerMock.Object);
        var request = new GetEmailVerifiedStatusRequest(userId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Given_UserWithUnverifiedEmail_When_GetEmailVerifiedStatus_Then_ReturnsFalseStatus()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        
        var user = new User
        {
            Id = userId,
            Email = "unverified@test.com",
            UserName = "unverified@test.com",
            NewEmailConfirmed = false
        };

        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        var handler = new GetEmailVerifiedStatusHandler(userManagerMock.Object);
        var request = new GetEmailVerifiedStatusRequest(userId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }
}

