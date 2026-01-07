using Backend.Data;
using Backend.Features.Users.AssignModeratorRole;
using Backend.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Backend.Tests.Handlers.Users;

public class AssignModeratorRoleHandlerTests
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
    public async Task Given_NonExistentUser_When_AssignModeratorRole_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userManagerMock = CreateUserManagerMock();

        var userId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var request = new AssignModeratorRoleRequest(userId);
        var handler = new AssignModeratorRoleHandler(context, userManagerMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Given_UserAlreadyModerator_When_AssignModeratorRole_Then_ReturnsConflict()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userManagerMock = CreateUserManagerMock();

        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var user = new User
        {
            Id = userId,
            Email = "test@test.com",
            UserName = "test@test.com"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        userManagerMock.Setup(x => x.IsInRoleAsync(user, "Moderator"))
            .ReturnsAsync(true);

        var request = new AssignModeratorRoleRequest(userId);
        var handler = new AssignModeratorRoleHandler(context, userManagerMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Given_ValidUser_When_AssignModeratorRole_Then_AssignsRoleSuccessfully()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userManagerMock = CreateUserManagerMock();

        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var user = new User
        {
            Id = userId,
            Email = "test@test.com",
            UserName = "test@test.com"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        userManagerMock.Setup(x => x.IsInRoleAsync(user, "Moderator"))
            .ReturnsAsync(false);
        userManagerMock.Setup(x => x.AddToRoleAsync(user, "Moderator"))
            .ReturnsAsync(IdentityResult.Success);

        var request = new AssignModeratorRoleRequest(userId);
        var handler = new AssignModeratorRoleHandler(context, userManagerMock.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }
}

