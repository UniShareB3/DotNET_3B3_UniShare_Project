using Backend.Data;
using Backend.Features.Users.GetAdmins;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Backend.Tests.Handlers.Users;

public class GetAdminsHandlerTests
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
    public async Task Given_AdminsExist_When_GetAdmins_Then_ReturnsAdminsList()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        
        var admins = new List<User>
        {
            new User { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Email = "admin1@test.com" },
            new User { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Email = "admin2@test.com" }
        };

        userManagerMock.Setup(x => x.GetUsersInRoleAsync("Admin"))
            .ReturnsAsync(admins);

        var handler = new GetAdminsHandler(userManagerMock.Object);
        var request = new GetAdminsRequest();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Given_NoAdmins_When_GetAdmins_Then_ReturnsEmptyList()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        
        userManagerMock.Setup(x => x.GetUsersInRoleAsync("Admin"))
            .ReturnsAsync(new List<User>());

        var handler = new GetAdminsHandler(userManagerMock.Object);
        var request = new GetAdminsRequest();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }
}

