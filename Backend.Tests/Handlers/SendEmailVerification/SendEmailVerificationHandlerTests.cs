using Backend.Data;
using Backend.Features.Shared.IAM.SendEmailVerification;
using Backend.Persistence;
using Backend.Services.EmailSender;
using Backend.Services.Hashing;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Backend.Tests.UnitTests;

public class SendEmailVerificationHandlerTests
{
    private readonly UserManager<User> _userManager;
    private readonly ApplicationContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IHashingService _hashingService;
    private readonly SendEmailVerificationHandler _handler;

    public SendEmailVerificationHandlerTests()
    {
        var store = Substitute.For<IUserStore<User>>();
        var options = Substitute.For<Microsoft.Extensions.Options.IOptions<IdentityOptions>>();
        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        var userValidators = new List<IUserValidator<User>>();
        var passwordValidators = new List<IPasswordValidator<User>>();
        var keyNormalizer = Substitute.For<ILookupNormalizer>();
        var errors = Substitute.For<IdentityErrorDescriber>();
        var services = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UserManager<User>>>();

        _userManager = Substitute.For<UserManager<User>>(
            store,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            services,
            logger);
        
        var dbOptions = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: Guid.Parse("10000000-0000-0000-0000-000000000001").ToString())
            .Options;
        _context = new ApplicationContext(dbOptions);
        
        _emailSender = Substitute.For<IEmailSender>();
        _hashingService = Substitute.For<IHashingService>();
        
        _handler = new SendEmailVerificationHandler(
            _userManager,
            _context,
            _emailSender,
            _hashingService);
    }

    [Fact]
    public async Task GivenValidUser_WhenSendEmailVerification_ThenSendsEmailAndReturnsOk()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            NewEmailConfirmed = false
        };
        
        _userManager.FindByIdAsync(userId.ToString()).Returns(Task.FromResult<User?>(user));
        _hashingService.HashCode(Arg.Any<string>()).Returns("hashed_code");
        _emailSender.SendEmailVerificationAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        var request = new SendEmailVerificationRequest(userId);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        await _emailSender.Received(1).SendEmailVerificationAsync(
            user.Email,
            Arg.Is<string>(code => code.Length == 6 ));
        
        var tokenInDb = await _context.EmailConfirmationTokens
            .FirstOrDefaultAsync(t => t.UserId == userId);
        tokenInDb.Should().NotBeNull();
        tokenInDb.IsUsed.Should().BeFalse();
    }

    [Fact]
    public async Task GivenUserNotFound_WhenSendEmailVerification_ThenReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        _userManager.FindByIdAsync(userId.ToString()).Returns(Task.FromResult<User?>(null));

        var request = new SendEmailVerificationRequest(userId);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        
        await _emailSender.DidNotReceive().SendEmailVerificationAsync(
            Arg.Any<string>(), 
            Arg.Any<string>());
    }

    [Fact]
    public async Task GivenEmailAlreadyConfirmed_WhenSendEmailVerification_ThenReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            NewEmailConfirmed = true
        };
        
        _userManager.FindByIdAsync(userId.ToString()).Returns(Task.FromResult<User?>(user));

        var request = new SendEmailVerificationRequest(userId);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        
        await _emailSender.DidNotReceive().SendEmailVerificationAsync(
            Arg.Any<string>(), 
            Arg.Any<string>());
    }

    [Fact]
    public async Task GivenValidUser_WhenEmailSendingFails_ThenReturnsProblem()
    {
        // Arrange
        var userId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            NewEmailConfirmed = false
        };
        
        _userManager.FindByIdAsync(userId.ToString()).Returns(Task.FromResult<User?>(user));
        _hashingService.HashCode(Arg.Any<string>()).Returns("hashed_code");
        _emailSender.SendEmailVerificationAsync(Arg.Any<string>(), Arg.Any<string>())
            .Throws(new Exception("SMTP connection failed"));

        var request = new SendEmailVerificationRequest(userId);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
    
}
