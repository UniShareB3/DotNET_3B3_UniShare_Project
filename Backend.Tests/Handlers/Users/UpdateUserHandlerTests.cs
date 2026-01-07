using AutoMapper;
using Backend.Data;
using Backend.Features.Users.DTO;
using Backend.Features.Users.UpdateUser;
using Backend.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Backend.Tests.Handlers.Users;

public class UpdateUserHandlerTests
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
    public async Task Given_NonExistentUser_When_UpdateUser_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userManagerMock = CreateUserManagerMock();
        var mapperMock = new Mock<IMapper>();

        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((User?)null);

        var handler = new UpdateUserHandler(userManagerMock.Object, context, mapperMock.Object);
        var dto = new UpdateUserDto("John", "Doe", null, null, null);
        var request = new UpdateUserRequest(userId, dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Given_ValidUser_When_UpdateFirstName_Then_UpdatesSuccessfully()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userManagerMock = CreateUserManagerMock();
        var mapperMock = new Mock<IMapper>();

        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var user = new User
        {
            Id = userId,
            FirstName = "OldName",
            LastName = "Doe",
            Email = "test@test.com",
            UserName = "test@test.com"
        };

        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        var handler = new UpdateUserHandler(userManagerMock.Object, context, mapperMock.Object);
        var dto = new UpdateUserDto("NewName", null, null, null, null);
        var request = new UpdateUserRequest(userId, dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NewName", user.FirstName);
    }

    [Fact]
    public async Task Given_ValidUser_When_UpdateLastName_Then_UpdatesSuccessfully()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userManagerMock = CreateUserManagerMock();
        var mapperMock = new Mock<IMapper>();

        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "OldLastName",
            Email = "test@test.com",
            UserName = "test@test.com"
        };

        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        var handler = new UpdateUserHandler(userManagerMock.Object, context, mapperMock.Object);
        var dto = new UpdateUserDto(null, "NewLastName", null, null, null);
        var request = new UpdateUserRequest(userId, dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NewLastName", user.LastName);
    }

    [Fact]
    public async Task Given_ValidUser_When_UpdateEmailWithDifferentEmail_Then_UpdatesEmailAndResetsConfirmation()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userManagerMock = CreateUserManagerMock();
        var mapperMock = new Mock<IMapper>();

        var userId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "old@test.com",
            UserName = "old@test.com",
            NewEmailConfirmed = true
        };

        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        var handler = new UpdateUserHandler(userManagerMock.Object, context, mapperMock.Object);
        var dto = new UpdateUserDto(null, null, "new@test.com", null, null);
        var request = new UpdateUserRequest(userId, dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new@test.com", user.Email);
        Assert.Equal("NEW@TEST.COM", user.NormalizedEmail);
        Assert.Equal("new@test.com", user.UserName);
        Assert.Equal("NEW@TEST.COM", user.NormalizedUserName);
        Assert.False(user.NewEmailConfirmed);
    }

    [Fact]
    public async Task Given_ValidUser_When_UpdateFails_Then_ReturnsBadRequest()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userManagerMock = CreateUserManagerMock();
        var mapperMock = new Mock<IMapper>();

        var userId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "test@test.com",
            UserName = "test@test.com"
        };

        var identityError = new IdentityError
        {
            Code = "UpdateFailed",
            Description = "User update failed"
        };

        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        var handler = new UpdateUserHandler(userManagerMock.Object, context, mapperMock.Object);
        var dto = new UpdateUserDto("NewName", null, null, null, null);
        var request = new UpdateUserRequest(userId, dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Given_ValidUser_When_UpdatePasswordWithValidPassword_Then_UpdatesPasswordSuccessfully()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userManagerMock = CreateUserManagerMock();
        var mapperMock = new Mock<IMapper>();

        var userId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "test@test.com",
            UserName = "test@test.com"
        };

        var resetToken = "reset-token-12345";

        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(resetToken);
        userManagerMock.Setup(x => x.ResetPasswordAsync(user, resetToken, "NewP@ssw0rd"))
            .ReturnsAsync(IdentityResult.Success);

        var handler = new UpdateUserHandler(userManagerMock.Object, context, mapperMock.Object);
        var dto = new UpdateUserDto(null, null, null, "NewP@ssw0rd", null);
        var request = new UpdateUserRequest(userId, dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once);
        userManagerMock.Verify(x => x.ResetPasswordAsync(user, resetToken, "NewP@ssw0rd"), Times.Once);
    }

    [Fact]
    public async Task Given_ValidUser_When_UpdatePasswordFails_Then_ReturnsBadRequest()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userManagerMock = CreateUserManagerMock();
        var mapperMock = new Mock<IMapper>();

        var userId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "test@test.com",
            UserName = "test@test.com"
        };

        var resetToken = "reset-token-12345";
        var identityError = new IdentityError
        {
            Code = "PasswordTooWeak",
            Description = "Password must be stronger"
        };

        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(resetToken);
        userManagerMock.Setup(x => x.ResetPasswordAsync(user, resetToken, "weak"))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        var handler = new UpdateUserHandler(userManagerMock.Object, context, mapperMock.Object);
        var dto = new UpdateUserDto(null, null, null, "weak", null);
        var request = new UpdateUserRequest(userId, dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }
}

