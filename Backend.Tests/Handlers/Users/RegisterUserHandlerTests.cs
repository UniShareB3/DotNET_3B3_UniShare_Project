using Backend.Data;
using Backend.Features.Users;
using Backend.Features.Users.Dtos;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Backend.Validators;
using FluentAssertions;
using k8s.KubeConfigModels;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Xunit;
using User = Backend.Data.User;

namespace Backend.Tests.Handlers.Users;

public class RegisterUserHandlerTests
{ 
    private static Mock<IMediator> CreateMediatorMock()
    {
        return new Mock<IMediator>();
    }

    private static Mock<IMapper> CreateMapperMock()
    {
        return new Mock<IMapper>();
    }

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        return new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null, null, null, null, null, null, null, null);
    }

    [Fact]
    public async Task Given_UserAlreadyExists_When_RegisterUser_Then_ReturnsError_WithDuplicateCode()
    {
        // Arrange
        var email = "johndoe@student.uaic.ro";
        var userDto = new RegisterUserDto("John", "Doe", email, "Password123!", "University");

        var userManagerMock = CreateUserManagerMock();
        var mediatorMock = CreateMediatorMock();
        var mapperMock = CreateMapperMock();
    
        var userEntity = new User { Email = email, UserName = email };
        mapperMock.Setup(m => m.Map<User>(It.IsAny<RegisterUserDto>())).Returns(userEntity);

        var expectedError = new IdentityError 
        { 
            Code = "DuplicateUserName", 
            Description = $"User name '{email}' is already taken." 
        };

        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(expectedError));
        
        ApplicationContext context = CreateInMemoryDbContext(System.Guid.NewGuid().ToString());

        var handler = new RegisterUserHandler(userManagerMock.Object, mediatorMock.Object, mapperMock.Object, context);

        // Act
        var result = await handler.Handle(new RegisterUserRequest(userDto), CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequest<IEnumerable<IdentityError>>>().Subject;

        badRequestResult.Value.Should().NotBeNull();
        badRequestResult.Value.Should().Contain(e => e.Code == "DuplicateUserName");
    }
    
    [Fact]
    public async Task Given_NewUser_When_RegisterUser_Then_ReturnsCreated_And_SendsEmail()
    {
        // Arrange
        var email = "newuser@student.uaic.ro";
        var userDto = new RegisterUserDto("New", "User", email, "ValidPass123!", "Alexandru Ioan Cuza University");
    
        var userManagerMock = CreateUserManagerMock();
        var mediatorMock = CreateMediatorMock();
        var mapperMock = CreateMapperMock();

        userManagerMock.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((User)null);

        var userEntity = new User { Email = email, UserName = email };
        mapperMock.Setup(m => m.Map<User>(userDto)).Returns(userEntity);

        userManagerMock.Setup(x => x.CreateAsync(userEntity, userDto.Password))
            .ReturnsAsync(IdentityResult.Success);
        
        ApplicationContext context = CreateInMemoryDbContext(System.Guid.NewGuid().ToString());
        context.Universities.Add(
            new University()
            {
                Name = "Alexandru Ioan Cuza University",
                EmailDomain = "student.uaic.ro",
                Id = System.Guid.NewGuid()
            });
        await context.SaveChangesAsync();
        
        var handler = new RegisterUserHandler(userManagerMock.Object, mediatorMock.Object, mapperMock.Object, context);

        // Act
        var result = await handler.Handle(new RegisterUserRequest(userDto), CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status201Created);
    }
    
    [Fact]
    public async Task Given_WeakPassword_When_RegisterUser_Then_ReturnsBadRequest_WithErrors()
    {
        // Arrange
        var email = "badpass@student.uaic.ro";
        var weakPassword = "123"; // Too short
        var userDto = new RegisterUserDto("John", "Doe", email, weakPassword, "University");

        var userManagerMock = CreateUserManagerMock();
        var mediatorMock = CreateMediatorMock();
        var mapperMock = CreateMapperMock();

        userManagerMock.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((User)null);

        var userEntity = new User { Email = email };
        mapperMock.Setup(m => m.Map<User>(userDto)).Returns(userEntity);

        var identityErrors = new[] { 
            new IdentityError { Code = "PasswordTooShort", Description = "Password is too short." } 
        };
    
        userManagerMock.Setup(x => x.CreateAsync(userEntity, weakPassword))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));
        
        ApplicationContext context = CreateInMemoryDbContext(System.Guid.NewGuid().ToString());

        var handler = new RegisterUserHandler(userManagerMock.Object, mediatorMock.Object, mapperMock.Object, context);

        // Act
        var result = await handler.Handle(new RegisterUserRequest(userDto), CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
    
    private static ApplicationContext CreateInMemoryDbContext(string guid)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: guid)
            .Options;

        var context = new ApplicationContext(options);
        return context;
    }
  

}