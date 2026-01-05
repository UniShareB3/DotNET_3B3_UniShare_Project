using AutoMapper;
using Backend.Data;
using Backend.Features.ModeratorAssignment.CreateModeratorAssignment;
using Backend.Features.ModeratorAssignment.DTO;
using Backend.Features.ModeratorAssignment.Enums;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Tests.Handlers.ModeratorAssignment;

public class CreateModeratorAssignmentHandlerTests
{
    private static ApplicationContext CreateInMemoryDbContext(string guid)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: guid)
            .Options;

        var context = new ApplicationContext(options);
        return context;
    }

    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CreateModeratorAssignmentDto, Data.ModeratorAssignment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewedByAdminId, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewedDate, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewedByAdmin, opt => opt.Ignore());

            cfg.CreateMap<Data.ModeratorAssignment, ModeratorAssignmentDto>();
        }, NullLoggerFactory.Instance);
        return config.CreateMapper();
    }

    [Fact]
    public async Task Given_ValidRequest_When_UserExistsAndNoPendingAssignment_Then_ReturnsCreatedResult()
    {
        // Arrange
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();

        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@example.com"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var dto = new CreateModeratorAssignmentDto(userId, "I want to help moderate the community");
        var request = new CreateModeratorAssignmentRequest(dto);

        var handler = new CreateModeratorAssignmentHandler(dbContext, mapper);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var createdResult = result as Created<ModeratorAssignmentDto>;
        createdResult.Should().NotBeNull();
        createdResult.Value.Should().NotBeNull();
        createdResult.Value.UserId.Should().Be(userId);
        createdResult.Value.Reason.Should().Be("I want to help moderate the community");
        createdResult.Value.Status.ToString().Should().Be(ModeratorAssignmentStatus.PENDING.ToString());

        // Verify it was saved to database
        var savedAssignment = await dbContext.ModeratorAssignments.FirstOrDefaultAsync(ma => ma.UserId == userId);
        savedAssignment.Should().NotBeNull();
        savedAssignment.Status.Should().Be(ModeratorAssignmentStatus.PENDING);
    }

    [Fact]
    public async Task Given_Request_When_UserDoesNotExist_Then_ReturnsNotFound()
    {
        // Arrange
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        var mapper = CreateMapper();

        var dto = new CreateModeratorAssignmentDto(userId, "I want to help moderate the community");
        var request = new CreateModeratorAssignmentRequest(dto);

        var handler = new CreateModeratorAssignmentHandler(dbContext, mapper);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Equals(Results.NotFound(new { message = "User not found" }));
    }

    [Fact]
    public async Task Given_Request_When_UserHasPendingAssignment_Then_ReturnsConflict()
    {
        // Arrange
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dbContext = CreateInMemoryDbContext( Guid.NewGuid().ToString());
        var mapper = CreateMapper();

        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@example.com"
        };
        dbContext.Users.Add(user);

        // Add existing pending assignment
        var existingAssignment = new Data.ModeratorAssignment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Reason = "Previous application",
            Status = ModeratorAssignmentStatus.PENDING,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };
        dbContext.ModeratorAssignments.Add(existingAssignment);
        await dbContext.SaveChangesAsync();

        var dto = new CreateModeratorAssignmentDto(userId, "I want to help moderate the community");
        var request = new CreateModeratorAssignmentRequest(dto);

        var handler = new CreateModeratorAssignmentHandler(dbContext, mapper);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Equals(Results.Conflict(new { message = "User already has a pending moderator assignment" }));
    }

    [Fact]
    public async Task Given_Request_When_UserHasRejectedAssignment_Then_ReturnsCreated()
    {
        // Arrange
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();

        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@example.com"
        };
        dbContext.Users.Add(user);

        // Add existing rejected assignment (should not block new application)
        var existingAssignment = new Data.ModeratorAssignment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Reason = "Previous application",
            Status = ModeratorAssignmentStatus.REJECTED,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };
        dbContext.ModeratorAssignments.Add(existingAssignment);
        await dbContext.SaveChangesAsync();

        var dto = new CreateModeratorAssignmentDto(userId, "I want to help moderate the community");
        var request = new CreateModeratorAssignmentRequest(dto);

        var handler = new CreateModeratorAssignmentHandler(dbContext, mapper);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var createdResult = result as Created<ModeratorAssignmentDto>;
        createdResult.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_Request_When_UserHasAcceptedAssignment_Then_ReturnsCreated()
    {
        // Arrange
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();

        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@example.com"
        };
        dbContext.Users.Add(user);

        // Add existing accepted assignment (should not block new application)
        var existingAssignment = new Data.ModeratorAssignment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Reason = "Previous application",
            Status = ModeratorAssignmentStatus.ACCEPTED,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };
        dbContext.ModeratorAssignments.Add(existingAssignment);
        await dbContext.SaveChangesAsync();

        var dto = new CreateModeratorAssignmentDto(userId, "I want to help moderate the community");
        var request = new CreateModeratorAssignmentRequest(dto);

        var handler = new CreateModeratorAssignmentHandler(dbContext, mapper);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var createdResult = result as Created<ModeratorAssignmentDto>;
        createdResult.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_ValidRequest_When_Created_Then_SetsCorrectDefaultValues()
    {
        // Arrange
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();

        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@example.com"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var dto = new CreateModeratorAssignmentDto(userId, "I want to help moderate the community");
        var request = new CreateModeratorAssignmentRequest(dto);

        var handler = new CreateModeratorAssignmentHandler(dbContext, mapper);
        var beforeCreation = DateTime.UtcNow;

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        var savedAssignment = await dbContext.ModeratorAssignments.FirstOrDefaultAsync(ma => ma.UserId == userId);
        savedAssignment.Should().NotBeNull();
        savedAssignment.Status.Should().Be(ModeratorAssignmentStatus.PENDING);
        savedAssignment.CreatedDate.Should().BeCloseTo(beforeCreation, TimeSpan.FromSeconds(5));
        savedAssignment.ReviewedByAdminId.Should().BeNull();
        savedAssignment.ReviewedDate.Should().BeNull();
    }

    [Fact]
    public async Task Given_ValidRequest_When_Created_Then_ReturnsCorrectLocation()
    {
        // Arrange
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();

        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@example.com"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var dto = new CreateModeratorAssignmentDto(userId, "I want to help moderate the community");
        var request = new CreateModeratorAssignmentRequest(dto);

        var handler = new CreateModeratorAssignmentHandler(dbContext, mapper);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var createdResult = result as Created<ModeratorAssignmentDto>;
        createdResult.Should().NotBeNull();
        createdResult.Location.Should().StartWith("/moderator-assignments/");
    }
}
