using Backend.Data;
using Backend.Features.Users;
using Backend.Features.Users.Dtos;
using Backend.Persistence;
using Backend.Validators;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Linq.Expressions;

namespace Backend.Tests.Validators;

// Helper classes for async enumerable support in tests
public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

    public ValueTask DisposeAsync() 
    { 
        _inner.Dispose(); 
        return ValueTask.CompletedTask; 
    }

    public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());

    public T Current => _inner.Current;
}

public class TestAsyncEnumerable<T> : IAsyncEnumerable<T>, IQueryable<T>
{
    private readonly IQueryable<T> _queryable;

    public TestAsyncEnumerable(IEnumerable<T> enumerable)
    {
        _queryable = enumerable.AsQueryable();
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(_queryable.GetEnumerator());

    public Type ElementType => _queryable.ElementType;
    public Expression Expression => _queryable.Expression;
    public IQueryProvider Provider => new TestAsyncQueryProvider<T>(_queryable.Provider);

    public IEnumerator<T> GetEnumerator() => _queryable.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class TestAsyncQueryProvider<T> : IQueryProvider
{
    private readonly IQueryProvider _inner;

    public TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<T>((IEnumerable<T>)_inner.CreateQuery<T>(expression));
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>((IEnumerable<TElement>)_inner.CreateQuery<TElement>(expression));
    public object? Execute(Expression expression) => _inner.Execute(expression);
    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);
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

        var userValidators = new List<IUserValidator<User>>();
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
            userValidators,
            passwordValidators,
            upperInvariantLookupNormalizer,
            errors,
            null,
            logger.Object);
    }

    private static UserManager<User> CreateUserManagerWithCustomPasswordValidator(
        IUserStore<User> store, 
        IPasswordValidator<User> customPasswordValidator)
    {
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions());

        var userValidators = new List<IUserValidator<User>>();
        var passwordValidators = new List<IPasswordValidator<User>>
        {
            customPasswordValidator
        };
        var passwordHasher = new PasswordHasher<User>();
        var upperInvariantLookupNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var logger = new Mock<ILogger<UserManager<User>>>();

        return new UserManager<User>(
            store,
            options.Object,
            passwordHasher,
            userValidators,
            passwordValidators,
            upperInvariantLookupNormalizer,
            errors,
            null,
            logger.Object);
    }

    #region FirstName Validation Tests

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

    #endregion

    #region LastName Validation Tests

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

    #endregion

    #region Email Uniqueness Validation Tests

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

    #endregion
    
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
    
}
