using Backend.Data;
using Backend.Persistence;
using Backend.Validators;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Linq.Expressions;
using Backend.Features.Users.DTO;
using Backend.Features.Users.UpdateUser;

namespace Backend.Tests.Validators;

// Helper classes for async enumerable support in tests
public class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public ValueTask DisposeAsync() 
    { 
        inner.Dispose(); 
        return ValueTask.CompletedTask; 
    }

    public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(inner.MoveNext());

    public T Current => inner.Current;
}

public class TestAsyncEnumerable<T>(IEnumerable<T> enumerable) : IAsyncEnumerable<T>, IQueryable<T>
{
    private readonly IQueryable<T> _queryable = enumerable.AsQueryable();

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(_queryable.GetEnumerator());

    public Type ElementType => _queryable.ElementType;
    public Expression Expression => _queryable.Expression;
    public IQueryProvider Provider => new TestAsyncQueryProvider<T>(_queryable.Provider);

    public IEnumerator<T> GetEnumerator() => _queryable.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class TestAsyncQueryProvider<T>(IQueryProvider inner) : IQueryProvider
{
    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<T>(inner.CreateQuery<T>(expression));
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(inner.CreateQuery<TElement>(expression));
    public object? Execute(Expression expression) => inner.Execute(expression);
    public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);
}

public class UpdateUserRequestValidatorTests
{
    private static ApplicationContext CreateInMemoryDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        return new ApplicationContext(options);
    }

    private static UserManager<User> CreateUserManager(IUserStore<User> store)
    {
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions());

        var passwordValidators = new List<IPasswordValidator<User>>
        {
            new PasswordValidator<User>()
        };
        var passwordHasher = new PasswordHasher<User>();
        var upperInvariantLookupNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var logger = new Mock<ILogger<UserManager<User>>>();

        return new UserManager<User>(
            store,
            options.Object,
            passwordHasher,
            new List<IUserValidator<User>>(),
            passwordValidators,
            upperInvariantLookupNormalizer,
            errors,
            null!,
            logger.Object);
    }

    [Fact]
    public async Task Given_FirstNameExceeds100Characters_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1");
        var userStore = new Mock<IUserStore<User>>();
        var userManager = CreateUserManager(userStore.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var longFirstName = new string('A', 101);
        var dto = new UpdateUserDto(longFirstName, null, null, null, null);
        var request = new UpdateUserRequest(Guid.Parse("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b2"), dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.UpdateUserDto.FirstName)
            .WithErrorMessage("First name cannot exceed 100 characters.");
    }

    [Fact]
    public async Task Given_FirstNameExactly100Characters_When_Validate_Then_NoValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1");
        var userStore = new Mock<IUserStore<User>>();
        var userManager = CreateUserManager(userStore.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var validFirstName = new string('A', 100);
        var dto = new UpdateUserDto(validFirstName, null, null, null, null);
        var request = new UpdateUserRequest(Guid.Parse("c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c2"), dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(r => r.UpdateUserDto.FirstName);
    }

    [Fact]
    public async Task Given_EmptyFirstName_When_Validate_Then_NoValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1");
        var userStore = new Mock<IUserStore<User>>();
        var userManager = CreateUserManager(userStore.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var dto = new UpdateUserDto("", null, null, null, null);
        var request = new UpdateUserRequest(Guid.Parse("d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d2"), dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(r => r.UpdateUserDto.FirstName);
    }

    [Fact]
    public async Task Given_NullFirstName_When_Validate_Then_NoValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1");
        var userStore = new Mock<IUserStore<User>>();
        var userManager = CreateUserManager(userStore.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var dto = new UpdateUserDto(null, null, null, null, null);
        var request = new UpdateUserRequest(Guid.Parse("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e2"), dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(r => r.UpdateUserDto.FirstName);
    }
    

    [Fact]
    public async Task Given_LastNameExceeds100Characters_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1");
        var userStore = new Mock<IUserStore<User>>();
        var userManager = CreateUserManager(userStore.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var longLastName = new string('B', 101);
        var dto = new UpdateUserDto(null, longLastName, null, null, null);
        var request = new UpdateUserRequest(Guid.Parse("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f2"), dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.UpdateUserDto.LastName)
            .WithErrorMessage("Last name cannot exceed 100 characters.");
    }

    [Fact]
    public async Task Given_LastNameExactly100Characters_When_Validate_Then_NoValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("a2a2a2a2-a2a2-a2a2-a2a2-a2a2a2a2a2a2");
        var userStore = new Mock<IUserStore<User>>();
        var userManager = CreateUserManager(userStore.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var validLastName = new string('B', 100);
        var dto = new UpdateUserDto(null, validLastName, null, null, null);
        var request = new UpdateUserRequest(Guid.Parse("a2a2a2a2-a2a2-a2a2-a2a2-a2a2a2a2a2a3"), dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(r => r.UpdateUserDto.LastName);
    }

    [Fact]
    public async Task Given_EmptyLastName_When_Validate_Then_NoValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2");
        var userStore = new Mock<IUserStore<User>>();
        var userManager = CreateUserManager(userStore.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var dto = new UpdateUserDto(null, "", null, null, null);
        var request = new UpdateUserRequest(Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b3"), dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(r => r.UpdateUserDto.LastName);
    }

    [Fact]
    public async Task Given_EmailAlreadyInUseByAnotherUser_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("c2c2c2c2-c2c2-c2c2-c2c2-c2c2c2c2c2c2");
        var existingUserId = Guid.Parse("c2c2c2c2-c2c2-c2c2-c2c2-c2c2c2c2c2c3");
        var currentUserId = Guid.Parse("c2c2c2c2-c2c2-c2c2-c2c2-c2c2c2c2c2c4");
        var existingEmail = "existing@student.uaic.ro";

        var existingUser = new User
        {
            Id = existingUserId,
            FirstName = "Existing",
            LastName = "User",
            Email = existingEmail,
            NormalizedEmail = existingEmail.ToUpperInvariant()
        };

        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        var userStoreMock = new Mock<IUserStore<User>>();
        userStoreMock.As<IUserEmailStore<User>>()
            .Setup(x => x.FindByEmailAsync(existingEmail.ToUpperInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var userManager = CreateUserManager(userStoreMock.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var dto = new UpdateUserDto(null, null, existingEmail, null, null);
        var request = new UpdateUserRequest(currentUserId, dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrors()
            .WithErrorMessage("Email is already in use by another user.");
    }

    [Fact]
    public async Task Given_EmailBelongsToSameUser_When_Validate_Then_NoValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("d2d2d2d2-d2d2-d2d2-d2d2-d2d2d2d2d2d2");
        var userId = Guid.Parse("d2d2d2d2-d2d2-d2d2-d2d2-d2d2d2d2d2d3");
        var email = "same@student.uaic.ro";

        var user = new User
        {
            Id = userId,
            FirstName = "Same",
            LastName = "User",
            Email = email,
            NormalizedEmail = email.ToUpperInvariant()
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var userStoreMock = new Mock<IUserStore<User>>();
        userStoreMock.As<IUserEmailStore<User>>()
            .Setup(x => x.FindByEmailAsync(email.ToUpperInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var userManager = CreateUserManager(userStoreMock.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var dto = new UpdateUserDto(null, null, email, null, null);
        var request = new UpdateUserRequest(userId, dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Given_NewUniqueEmail_When_Validate_Then_NoValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("e2e2e2e2-e2e2-e2e2-e2e2-e2e2e2e2e2e2");
        var userId = Guid.Parse("e2e2e2e2-e2e2-e2e2-e2e2-e2e2e2e2e2e3");
        var newEmail = "newemail@student.uaic.ro";

        var userStoreMock = new Mock<IUserStore<User>>();
        userStoreMock.As<IUserEmailStore<User>>()
            .Setup(x => x.FindByEmailAsync(newEmail.ToUpperInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var userManager = CreateUserManager(userStoreMock.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var dto = new UpdateUserDto(null, null, newEmail, null, null);
        var request = new UpdateUserRequest(userId, dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Given_EmptyEmail_When_Validate_Then_NoValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("f2f2f2f2-f2f2-f2f2-f2f2-f2f2f2f2f2f2");
        var userStore = new Mock<IUserStore<User>>();
        var userManager = CreateUserManager(userStore.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var dto = new UpdateUserDto(null, null, "", null, null);
        var request = new UpdateUserRequest(Guid.Parse("f2f2f2f2-f2f2-f2f2-f2f2-f2f2f2f2f2f3"), dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public async Task Given_EmptyPassword_When_Validate_Then_NoValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("02020202-0202-0202-0202-020202020202");
        var userStore = new Mock<IUserStore<User>>();
        var userManager = CreateUserManager(userStore.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var dto = new UpdateUserDto(null, null, null, "", null);
        var request = new UpdateUserRequest(Guid.Parse("02020202-0202-0202-0202-020202020203"), dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Given_NullPassword_When_Validate_Then_NoValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("03030303-0303-0303-0303-030303030303");
        var userStore = new Mock<IUserStore<User>>();
        var userManager = CreateUserManager(userStore.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var dto = new UpdateUserDto(null, null, null, null, null);
        var request = new UpdateUserRequest(Guid.Parse("03030303-0303-0303-0303-030303030304"), dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    

    [Fact]
    public async Task Given_NonExistentUniversity_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("11111111-1111-1111-1111-111111111112");
        var userStore = new Mock<IUserStore<User>>();
        var userManager = CreateUserManager(userStore.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var nonExistentUniversity = "NonExistent University";
        var dto = new UpdateUserDto(null, null, null, null, nonExistentUniversity);
        var request = new UpdateUserRequest(Guid.Parse("11111111-1111-1111-1111-111111111113"), dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrors()
            .WithErrorMessage($"University '{nonExistentUniversity}' does not exist.");
    }

    [Fact]
    public async Task Given_ExistingUniversity_When_Validate_Then_NoValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("11111111-1111-1111-1111-111111111111");
        var universityName = "Alexandru Ioan Cuza University";

        var university = new University
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111114"),
            Name = universityName,
            EmailDomain = "student.uaic.ro"
        };

        context.Universities.Add(university);
        await context.SaveChangesAsync();

        var userStore = new Mock<IUserStore<User>>();
        var userManager = CreateUserManager(userStore.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var dto = new UpdateUserDto(null, null, null, null, universityName);
        var request = new UpdateUserRequest(Guid.Parse("11111111-1111-1111-1111-111111111115"), dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Given_EmptyUniversityName_When_Validate_Then_NoValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("a3a3a3a3-a3a3-a3a3-a3a3-a3a3a3a3a3a3");
        var userStore = new Mock<IUserStore<User>>();
        var userManager = CreateUserManager(userStore.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var dto = new UpdateUserDto(null, null, null, null, "");
        var request = new UpdateUserRequest(Guid.Parse("a3a3a3a3-a3a3-a3a3-a3a3-a3a3a3a3a3a4"), dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Given_NullUniversityName_When_Validate_Then_NoValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3");
        var userStore = new Mock<IUserStore<User>>();
        var userManager = CreateUserManager(userStore.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var dto = new UpdateUserDto(null, null, null, null, null);
        var request = new UpdateUserRequest(Guid.Parse("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b4"), dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    

    [Fact]
    public async Task Given_MultipleValidationErrors_When_Validate_Then_ReturnsAllErrors()
    {
        // Arrange
        var context = CreateInMemoryDbContext("c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3");
        var userStore = new Mock<IUserStore<User>>();
        var userManager = CreateUserManager(userStore.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var longFirstName = new string('A', 101);
        var longLastName = new string('B', 101);
        var nonExistentUniversity = "NonExistent University";

        var dto = new UpdateUserDto(longFirstName, longLastName, null, null, nonExistentUniversity);
        var request = new UpdateUserRequest(Guid.Parse("c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c4"), dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.UpdateUserDto.FirstName);
        result.ShouldHaveValidationErrorFor(r => r.UpdateUserDto.LastName);
        result.ShouldHaveValidationErrors()
            .WithErrorMessage($"University '{nonExistentUniversity}' does not exist.");
    }

    [Fact]
    public async Task Given_ValidUpdateRequest_When_Validate_Then_NoValidationErrors()
    {
        // Arrange
        var context = CreateInMemoryDbContext("d3d3d3d3-d3d3-d3d3-d3d3-d3d3d3d3d3d3");
        var universityName = "Test University";

        var university = new University
        {
            Id = Guid.Parse("d3d3d3d3-d3d3-d3d3-d3d3-d3d3d3d3d3d4"),
            Name = universityName,
            EmailDomain = "student.uaic.ro"
        };

        context.Universities.Add(university);
        await context.SaveChangesAsync();

        var userStore = new Mock<IUserStore<User>>();
        var userManager = CreateUserManager(userStore.Object);
        var validator = new UpdateUserRequestValidator(context, userManager);

        var dto = new UpdateUserDto("John", "Doe", null, null, universityName);
        var request = new UpdateUserRequest(Guid.Parse("d3d3d3d3-d3d3-d3d3-d3d3-d3d3d3d3d3d5"), dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
 
    [Fact]
    public async Task Given_WeakPasswordWithoutDigit_When_ValidatingPasswordStrength_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("e3e3e3e3-e3e3-e3e3-e3e3-e3e3e3e3e3e3");
        var userId = Guid.Parse("e3e3e3e3-e3e3-e3e3-e3e3-e3e3e3e3e3e4");
        
        var user = new User
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "testuser@student.uaic.ro",
            UserName = "testuser@student.uaic.ro"
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        var users = new List<User> { user };
        var asyncEnumerable = new TestAsyncEnumerable<User>(users);
        
        var userStoreMock = new Mock<IUserStore<User>>();
        
        var queryableStoreMock = userStoreMock.As<IQueryableUserStore<User>>();
        queryableStoreMock.Setup(x => x.Users).Returns(asyncEnumerable);
        
        // Setup IUserEmailStore
        userStoreMock.As<IUserEmailStore<User>>()
            .Setup(x => x.FindByIdAsync(userId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        
        // Setup IUserPasswordStore
        userStoreMock.As<IUserPasswordStore<User>>()
            .Setup(x => x.GetPasswordHashAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u.PasswordHash);

        var passwordValidators = new List<IPasswordValidator<User>>
        {
            new PasswordValidator<User>()
        };

        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions
        {
            Password = new PasswordOptions
            {
                RequireDigit = true,
                RequiredLength = 6,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireNonAlphanumeric = true
            }
        });

        var passwordHasher = new PasswordHasher<User>();
        var logger = new Mock<ILogger<UserManager<User>>>();
        
        var userManager = new UserManager<User>(
            userStoreMock.Object,
            options.Object,
            passwordHasher,
            new List<IUserValidator<User>>(),
            passwordValidators,
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            logger.Object);

        var validator = new UpdateUserRequestValidator(context, userManager);
        
        // Password without a digit: "Password!"
        var dto = new UpdateUserDto(null, null, null, "Password!", null);
        var request = new UpdateUserRequest(userId, dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Password");
    }

    [Fact]
    public async Task Given_WeakPasswordTooShort_When_ValidatingPasswordStrength_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("f3f3f3f3-f3f3-f3f3-f3f3-f3f3f3f3f3f3");
        var userId = Guid.Parse("f3f3f3f3-f3f3-f3f3-f3f3-f3f3f3f3f3f4");
        
        var user = new User
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "testuser2@student.uaic.ro",
            UserName = "testuser2@student.uaic.ro"
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        var users = new List<User> { user };
        var asyncEnumerable = new TestAsyncEnumerable<User>(users);
        
        var userStoreMock = new Mock<IUserStore<User>>();
        
        var queryableStoreMock = userStoreMock.As<IQueryableUserStore<User>>();
        queryableStoreMock.Setup(x => x.Users).Returns(asyncEnumerable);
        
        userStoreMock.As<IUserEmailStore<User>>()
            .Setup(x => x.FindByIdAsync(userId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        
        userStoreMock.As<IUserPasswordStore<User>>()
            .Setup(x => x.GetPasswordHashAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u.PasswordHash);

        var passwordValidators = new List<IPasswordValidator<User>>
        {
            new PasswordValidator<User>()
        };

        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions
        {
            Password = new PasswordOptions
            {
                RequireDigit = true,
                RequiredLength = 6,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireNonAlphanumeric = true
            }
        });

        var passwordHasher = new PasswordHasher<User>();
        var logger = new Mock<ILogger<UserManager<User>>>();
        
        var userManager = new UserManager<User>(
            userStoreMock.Object,
            options.Object,
            passwordHasher,
            new List<IUserValidator<User>>(),
            passwordValidators,
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            logger.Object);

        var validator = new UpdateUserRequestValidator(context, userManager);
        
        //the Password is too short: "Pa1!"
        var dto = new UpdateUserDto(null, null, null, "Pa1!", null);
        var request = new UpdateUserRequest(userId, dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Password");
    }

    [Fact]
    public async Task Given_PasswordAlreadyInDatabase_When_ValidatingPasswordNotInDatabase_Then_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("a4a4a4a4-a4a4-a4a4-a4a4-a4a4a4a4a4a4");
        var currentUserId = Guid.Parse("a4a4a4a4-a4a4-a4a4-a4a4-a4a4a4a4a4a5");
        var existingUserId = Guid.Parse("a4a4a4a4-a4a4-a4a4-a4a4-a4a4a4a4a4a6");
        
        var existingPassword = "ExistingP@ssw0rd";
        var passwordHasher = new PasswordHasher<User>();
        
        var existingUser = new User
        {
            Id = existingUserId,
            FirstName = "Existing",
            LastName = "User",
            Email = "existing@student.uaic.ro",
            UserName = "existing@student.uaic.ro",
            PasswordHash = passwordHasher.HashPassword(null!, existingPassword)
        };
        
        var currentUser = new User
        {
            Id = currentUserId,
            FirstName = "Current",
            LastName = "User",
            Email = "current@student.uaic.ro",
            UserName = "current@student.uaic.ro"
        };
        
        context.Users.Add(existingUser);
        context.Users.Add(currentUser);
        await context.SaveChangesAsync();

        // Create a combined mock for IUserStore, IQueryableUserStore, and IUserPasswordStore
        var users = new List<User> { existingUser, currentUser };
        var asyncEnumerable = new TestAsyncEnumerable<User>(users);
        
        var userStoreMock = new Mock<IUserStore<User>>();
        
        // Setup IQueryableUserStore
        var queryableStoreMock = userStoreMock.As<IQueryableUserStore<User>>();
        queryableStoreMock.Setup(x => x.Users).Returns(asyncEnumerable);
        
        // Setup IUserEmailStore
        userStoreMock.As<IUserEmailStore<User>>()
            .Setup(x => x.FindByIdAsync(currentUserId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);
        
        // Setup IUserPasswordStore
        userStoreMock.As<IUserPasswordStore<User>>()
            .Setup(x => x.GetPasswordHashAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u.PasswordHash);

        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions());

        var logger = new Mock<ILogger<UserManager<User>>>();
        
        var userManager = new UserManager<User>(
            userStoreMock.Object,
            options.Object,
            passwordHasher,
            new List<IUserValidator<User>>(),
            new List<IPasswordValidator<User>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            logger.Object);


        var validator = new UpdateUserRequestValidator(context, userManager);
        
        // Try to use the same password as existing user
        var dto = new UpdateUserDto(null, null, null, existingPassword, null);
        var request = new UpdateUserRequest(currentUserId, dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Password")
            .WithErrorMessage("This password is already in use. Please choose a different password.");
    }

    [Fact]
    public async Task Given_UniqueStrongPassword_When_ValidatingPassword_Then_NoValidationError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("b4b4b4b4-b4b4-b4b4-b4b4-b4b4b4b4b4b4");
        var userId = Guid.Parse("b4b4b4b4-b4b4-b4b4-b4b4-b4b4b4b4b4b5");
        
        var passwordHasher = new PasswordHasher<User>();
        
        var user = new User
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "testunique@student.uaic.ro",
            UserName = "testunique@student.uaic.ro"
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        var users = new List<User> { user };
        var asyncEnumerable = new TestAsyncEnumerable<User>(users);
        
        var userStoreMock = new Mock<IUserStore<User>>();
        
        var queryableStoreMock = userStoreMock.As<IQueryableUserStore<User>>();
        queryableStoreMock.Setup(x => x.Users).Returns(asyncEnumerable);
        
        userStoreMock.As<IUserEmailStore<User>>()
            .Setup(x => x.FindByIdAsync(userId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        
        userStoreMock.As<IUserPasswordStore<User>>()
            .Setup(x => x.GetPasswordHashAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u.PasswordHash);

        var passwordValidators = new List<IPasswordValidator<User>>
        {
            new PasswordValidator<User>()
        };

        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions
        {
            Password = new PasswordOptions
            {
                RequireDigit = true,
                RequiredLength = 6,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireNonAlphanumeric = true
            }
        });

        var logger = new Mock<ILogger<UserManager<User>>>();
        
        var userManager = new UserManager<User>(
            userStoreMock.Object,
            options.Object,
            passwordHasher,
            new List<IUserValidator<User>>(),
            passwordValidators,
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            logger.Object);


        var validator = new UpdateUserRequestValidator(context, userManager);
        
        var dto = new UpdateUserDto(null, null, null, "UniqueP@ssw0rd123", null);
        var request = new UpdateUserRequest(userId, dto);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor("Password");
    }
    
}
