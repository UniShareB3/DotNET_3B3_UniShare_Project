using Backend.Data;
using Backend.Features.ModeratorAssignment.CreateModeratorAssignment;
using Backend.Features.ModeratorAssignment.DTO;
using Backend.Persistence;
using Backend.Validators;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using ModeratorAssignment = Backend.Data.ModeratorAssignment;

namespace Backend.Tests.Validators;

public class CreateModeratorAssignmentValidatorTests
{
    
    private static ApplicationContext CreateInMemoryDbContext(string guid)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: guid)
            .Options;

        var context = new ApplicationContext(options);
        return context;
    }
    
    private Mock<UserManager<User>> GetMockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        var mgr = new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        return mgr;
    }

    [Fact]
    public async Task Given_ValidRequest_When_NoExistingAssignment_Then_ReturnsValid()
    {
        // Arrange
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        var dto = new CreateModeratorAssignmentDto(userId, "I want to help moderate the community");
        var request = new CreateModeratorAssignmentRequest(dto);
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        
        Mock<UserManager<User>> mockUserManager = GetMockUserManager();
        mockUserManager.Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(new User { Id = userId, UserName = "testuser" });
        mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { "User" });
        
        var dtoValidator = new CreateModeratorAssignmentDtoValidator(dbContext, mockUserManager.Object);
        var validator = new CreateModeratorAssignmentRequestValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert 
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_Request_When_UserIdIsEmpty_Then_ReturnsValidationError()
    {
        // Arrange
        
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        CreateModeratorAssignmentDto dto = new CreateModeratorAssignmentDto(Guid.Empty, "Valid reason");
        CreateModeratorAssignmentRequest request = new CreateModeratorAssignmentRequest(dto);
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        
        Mock<UserManager<User>> mockUserManager = GetMockUserManager();
        mockUserManager.Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(new User { Id = userId, UserName = "testuser" });
        mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { "User" });
        
        var dtoValidator = new CreateModeratorAssignmentDtoValidator(dbContext, mockUserManager.Object);
        var validator = new CreateModeratorAssignmentRequestValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert 
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "User ID is required");
    }

    [Fact]
    public async Task Given_Request_When_ReasonIsEmpty_Then_ReturnsValidationError()
    {
        // Arrange
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        CreateModeratorAssignmentDto dto = new CreateModeratorAssignmentDto(userId, "");
        CreateModeratorAssignmentRequest request = new CreateModeratorAssignmentRequest(dto);
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        
        Mock<UserManager<User>> mockUserManager = GetMockUserManager();
        mockUserManager.Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(new User { Id = userId, UserName = "testuser" });
        mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { "User" });
        
        var dtoValidator = new CreateModeratorAssignmentDtoValidator(dbContext, mockUserManager.Object);
        var validator = new CreateModeratorAssignmentRequestValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert 
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Reason is required");
    }

    [Fact]
    public async Task Given_Request_When_ReasonExceedsMaxLength_Then_ReturnsValidationError()
    {
        // Arrange
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        string longReason = new string('A', 1001); // 1001 characters
        
        CreateModeratorAssignmentDto dto = new CreateModeratorAssignmentDto(userId, longReason);
        CreateModeratorAssignmentRequest request = new CreateModeratorAssignmentRequest(dto);
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        
        Mock<UserManager<User>> mockUserManager = GetMockUserManager();
        mockUserManager.Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(new User { Id = userId, UserName = "testuser" });
        mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { "User" });
        
        var dtoValidator = new CreateModeratorAssignmentDtoValidator(dbContext, mockUserManager.Object);
        var validator = new CreateModeratorAssignmentRequestValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert 
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Reason cannot exceed 1000 characters");
    }

    [Fact]
    public async Task Given_Request_When_DeclinePreviously_Then_ReturnsValidationError()
    {
        // Arrange
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        CreateModeratorAssignmentDto dto = new CreateModeratorAssignmentDto(userId, "Violation of community guidelines");
        CreateModeratorAssignmentRequest request = new CreateModeratorAssignmentRequest(dto);
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        
        var existingAssignment = new ModeratorAssignment()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Reason = "Previous violation",
            Status = Features.ModeratorAssignment.Enums.ModeratorAssignmentStatus.REJECTED,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };
        
        dbContext.ModeratorAssignments.Add(existingAssignment);
        await dbContext.SaveChangesAsync();
        
        Mock<UserManager<User>> mockUserManager = GetMockUserManager();
        mockUserManager.Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(new User { Id = userId, UserName = "testuser" });
        mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { "User" });
        
        var dtoValidator = new CreateModeratorAssignmentDtoValidator(dbContext, mockUserManager.Object);
        var validator = new CreateModeratorAssignmentRequestValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert 
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "A month must pass between submitting a moderator assignment again");
    }

    [Fact]
    public async Task Given_Request_When_DeclinedMoreThan30DaysAgo_Then_ReturnsValid()
    {
        // Arrange
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        CreateModeratorAssignmentDto dto = new CreateModeratorAssignmentDto(userId, "I want to help moderate");
        CreateModeratorAssignmentRequest request = new CreateModeratorAssignmentRequest(dto);
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        
        var existingAssignment = new ModeratorAssignment()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Reason = "Previous violation",
            Status = Features.ModeratorAssignment.Enums.ModeratorAssignmentStatus.REJECTED,
            CreatedDate = DateTime.UtcNow.AddDays(-31)
        };
        
        dbContext.ModeratorAssignments.Add(existingAssignment);
        await dbContext.SaveChangesAsync();
        
        Mock<UserManager<User>> mockUserManager = GetMockUserManager();
        mockUserManager.Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(new User { Id = userId, UserName = "testuser" });
        mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { "User" });
        
        var dtoValidator = new CreateModeratorAssignmentDtoValidator(dbContext, mockUserManager.Object);
        var validator = new CreateModeratorAssignmentRequestValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert 
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_Request_When_UserIsAlreadyModerator_Then_ReturnsValidationError()
    {
        // Arrange
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        CreateModeratorAssignmentDto dto = new CreateModeratorAssignmentDto(userId, "I want to help moderate");
        CreateModeratorAssignmentRequest request = new CreateModeratorAssignmentRequest(dto);
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        
        Mock<UserManager<User>> mockUserManager = GetMockUserManager();
        mockUserManager.Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(new User { Id = userId, UserName = "testuser" });
        mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { "Moderator" });
        
        var dtoValidator = new CreateModeratorAssignmentDtoValidator(dbContext, mockUserManager.Object);
        var validator = new CreateModeratorAssignmentRequestValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert 
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "You already have a moderator assignment");
    }
    
    [Fact]
    public async Task Given_Request_When_PendingStatus_Then_ReturnsError()
    {
        // Arrange
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        CreateModeratorAssignmentDto dto = new CreateModeratorAssignmentDto(userId, "I want to help moderate");
        CreateModeratorAssignmentRequest request = new CreateModeratorAssignmentRequest(dto);
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        
        // Existing pending assignment shouldn't block a new one (validation only checks REJECTED status)
        var existingAssignment = new ModeratorAssignment()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Reason = "Previous application",
            Status = Features.ModeratorAssignment.Enums.ModeratorAssignmentStatus.PENDING,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };
        
        dbContext.ModeratorAssignments.Add(existingAssignment);
        await dbContext.SaveChangesAsync();
        
        Mock<UserManager<User>> mockUserManager = GetMockUserManager();
        mockUserManager.Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(new User { Id = userId, UserName = "testuser" });
        mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { "User" });
        
        var dtoValidator = new CreateModeratorAssignmentDtoValidator(dbContext, mockUserManager.Object);
        var validator = new CreateModeratorAssignmentRequestValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert 
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "You already have a pending moderator assignment request");
    }
}