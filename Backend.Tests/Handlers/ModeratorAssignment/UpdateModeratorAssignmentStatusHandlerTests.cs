using AutoMapper;
using Backend.Data;
using Backend.Features.ModeratorAssignment.DTO;
using Backend.Features.ModeratorAssignment.Enums;
using Backend.Features.ModeratorAssignment.UpdateModeratorAssignment;
using Backend.Features.Reports.Enums;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Backend.Tests.Handlers.ModeratorAssignment;

public class UpdateModeratorAssignmentStatusHandlerTests
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
            cfg.CreateMap<Data.ModeratorAssignment, ModeratorAssignmentDto>();
        }, NullLoggerFactory.Instance);

        return config.CreateMapper();
    }

    private Mock<UserManager<User>> GetMockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        var mgr = new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        return mgr;
    }

    [Fact]
    public async Task Given_ValidRequest_When_AssignmentExistsAndAdminExists_Then_ReturnsOk()
    {
        // Arrange
        Guid assignmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid adminId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        var mapper = CreateMapper();
        var mockUserManager = GetMockUserManager();

        var user = new User { Id = userId, UserName = "testuser", Email = "test@example.com" };
        var admin = new User { Id = adminId, UserName = "admin", Email = "admin@example.com" };
        
        dbContext.Users.AddRange(user, admin);

        var assignment = new Data.ModeratorAssignment
        {
            Id = assignmentId,
            UserId = userId,
            Reason = "I want to help moderate",
            Status = ModeratorAssignmentStatus.PENDING,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };
        dbContext.ModeratorAssignments.Add(assignment);
        await dbContext.SaveChangesAsync();

        var dto = new UpdateModeratorAssignmentStatusDto(ModeratorAssignmentStatus.REJECTED, adminId);
        var request = new UpdateModeratorAssignmentStatusRequest(assignmentId, dto);

        var handler = new UpdateModeratorAssignmentStatusHandler(dbContext, mapper, mockUserManager.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as Ok<ModeratorAssignmentDto>;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().NotBeNull();
        okResult.Value.Status.Should().Be("REJECTED");

        // Verify database was updated
        var updatedAssignment = await dbContext.ModeratorAssignments.FindAsync(assignmentId);
        updatedAssignment.Should().NotBeNull();
        updatedAssignment!.Status.Should().Be(ModeratorAssignmentStatus.REJECTED);
        updatedAssignment.ReviewedByAdminId.Should().Be(adminId);
        updatedAssignment.ReviewedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_Request_When_AssignmentDoesNotExist_Then_ReturnsNotFound()
    {
        // Arrange
        Guid assignmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid adminId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();
        var mockUserManager = GetMockUserManager();

        var dto = new UpdateModeratorAssignmentStatusDto(ModeratorAssignmentStatus.ACCEPTED, adminId);
        var request = new UpdateModeratorAssignmentStatusRequest(assignmentId, dto);

        var handler = new UpdateModeratorAssignmentStatusHandler(dbContext, mapper, mockUserManager.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Equals(Results.NotFound(new { message = "Moderator assignment not found" }));
    }
    

    [Fact]
    public async Task Given_AcceptedStatus_When_UserExists_Then_AssignsModeratorRole()
    {
        // Arrange
        Guid assignmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid adminId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        var mapper = CreateMapper();
        var mockUserManager = GetMockUserManager();

        var user = new User { Id = userId, UserName = "testuser", Email = "test@example.com" };
        var admin = new User { Id = adminId, UserName = "admin", Email = "admin@example.com" };
        
        dbContext.Users.AddRange(user, admin);

        var assignment = new Data.ModeratorAssignment
        {
            Id = assignmentId,
            UserId = userId,
            Reason = "I want to help moderate",
            Status = ModeratorAssignmentStatus.PENDING,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };
        dbContext.ModeratorAssignments.Add(assignment);
        await dbContext.SaveChangesAsync();

        mockUserManager.Setup(um => um.IsInRoleAsync(user, "Moderator"))
            .ReturnsAsync(false);
        mockUserManager.Setup(um => um.AddToRoleAsync(user, "Moderator"))
            .ReturnsAsync(IdentityResult.Success);

        var dto = new UpdateModeratorAssignmentStatusDto(ModeratorAssignmentStatus.ACCEPTED, adminId);
        var request = new UpdateModeratorAssignmentStatusRequest(assignmentId, dto);

        var handler = new UpdateModeratorAssignmentStatusHandler(dbContext, mapper, mockUserManager.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as Ok<ModeratorAssignmentDto>;
        okResult.Should().NotBeNull();

        mockUserManager.Verify(um => um.IsInRoleAsync(user, "Moderator"), Times.Once);
        mockUserManager.Verify(um => um.AddToRoleAsync(user, "Moderator"), Times.Once);
    }

    [Fact]
    public async Task Given_AcceptedStatus_When_UserAlreadyHasModeratorRole_Then_DoesNotAssignRoleAgain()
    {
        // Arrange
        Guid assignmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid adminId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        var mapper = CreateMapper();
        var mockUserManager = GetMockUserManager();

        var user = new User { Id = userId, UserName = "testuser", Email = "test@example.com" };
        var admin = new User { Id = adminId, UserName = "admin", Email = "admin@example.com" };
        
        dbContext.Users.AddRange(user, admin);

        var assignment = new Data.ModeratorAssignment
        {
            Id = assignmentId,
            UserId = userId,
            Reason = "I want to help moderate",
            Status = ModeratorAssignmentStatus.PENDING,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };
        dbContext.ModeratorAssignments.Add(assignment);
        await dbContext.SaveChangesAsync();

        mockUserManager.Setup(um => um.IsInRoleAsync(user, "Moderator"))
            .ReturnsAsync(true);

        var dto = new UpdateModeratorAssignmentStatusDto(ModeratorAssignmentStatus.ACCEPTED, adminId);
        var request = new UpdateModeratorAssignmentStatusRequest(assignmentId, dto);

        var handler = new UpdateModeratorAssignmentStatusHandler(dbContext, mapper, mockUserManager.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        mockUserManager.Verify(um => um.IsInRoleAsync(user, "Moderator"), Times.Once);
        mockUserManager.Verify(um => um.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Given_AcceptedStatus_When_PendingReportsExist_Then_ReassignsReportsToNewModerator()
    {
        // Arrange
        Guid assignmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid adminId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Guid adminRoleId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        var mapper = CreateMapper();
        var mockUserManager = GetMockUserManager();

        var user = new User { Id = userId, UserName = "newmoderator", Email = "mod@example.com" };
        var admin = new User { Id = adminId, UserName = "admin", Email = "admin@example.com" };
        
        dbContext.Users.AddRange(user, admin);

        // Add Admin role
        var adminRole = new IdentityRole<Guid> { Id = adminRoleId, Name = "Admin", NormalizedName = "ADMIN" };
        dbContext.Roles.Add(adminRole);

        // Add admin to Admin role
        var userRole = new IdentityUserRole<Guid> { UserId = adminId, RoleId = adminRoleId };
        dbContext.UserRoles.Add(userRole);

        var assignment = new Data.ModeratorAssignment
        {
            Id = assignmentId,
            UserId = userId,
            Reason = "I want to help moderate",
            Status = ModeratorAssignmentStatus.PENDING,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };
        dbContext.ModeratorAssignments.Add(assignment);

        // Add pending reports assigned to admin
        for (int i = 0; i < 7; i++)
        {
            var report = new Report
            {
                Id = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Description = $"Report {i}",
                Status = ReportStatus.PENDING,
                ModeratorId = adminId,
                CreatedDate = DateTime.UtcNow.AddDays(-i)
            };
            dbContext.Reports.Add(report);
        }

        await dbContext.SaveChangesAsync();

        mockUserManager.Setup(um => um.IsInRoleAsync(user, "Moderator"))
            .ReturnsAsync(false);
        mockUserManager.Setup(um => um.AddToRoleAsync(user, "Moderator"))
            .ReturnsAsync(IdentityResult.Success);

        var dto = new UpdateModeratorAssignmentStatusDto(ModeratorAssignmentStatus.ACCEPTED, adminId);
        var request = new UpdateModeratorAssignmentStatusRequest(assignmentId, dto);

        var handler = new UpdateModeratorAssignmentStatusHandler(dbContext, mapper, mockUserManager.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        
        // Verify that up to 5 reports were reassigned to the new moderator
        var reassignedReports = await dbContext.Reports
            .Where(r => r.ModeratorId == userId)
            .ToListAsync();
        
        reassignedReports.Should().HaveCount(5);
    }

    [Fact]
    public async Task Given_RejectedStatus_When_Updated_Then_DoesNotAssignModeratorRole()
    {
        // Arrange
        Guid assignmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid adminId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        var mapper = CreateMapper();
        var mockUserManager = GetMockUserManager();

        var user = new User { Id = userId, UserName = "testuser", Email = "test@example.com" };
        var admin = new User { Id = adminId, UserName = "admin", Email = "admin@example.com" };
        
        dbContext.Users.AddRange(user, admin);

        var assignment = new Data.ModeratorAssignment
        {
            Id = assignmentId,
            UserId = userId,
            Reason = "I want to help moderate",
            Status = ModeratorAssignmentStatus.PENDING,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };
        dbContext.ModeratorAssignments.Add(assignment);
        await dbContext.SaveChangesAsync();

        var dto = new UpdateModeratorAssignmentStatusDto(ModeratorAssignmentStatus.REJECTED, adminId);
        var request = new UpdateModeratorAssignmentStatusRequest(assignmentId, dto);

        var handler = new UpdateModeratorAssignmentStatusHandler(dbContext, mapper, mockUserManager.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        mockUserManager.Verify(um => um.IsInRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        mockUserManager.Verify(um => um.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Given_AcceptedStatus_When_Updated_Then_SetsReviewedDateAndAdmin()
    {
        // Arrange
        Guid assignmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid adminId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        var mapper = CreateMapper();
        var mockUserManager = GetMockUserManager();

        var user = new User { Id = userId, UserName = "testuser", Email = "test@example.com" };
        var admin = new User { Id = adminId, UserName = "admin", Email = "admin@example.com" };
        
        dbContext.Users.AddRange(user, admin);

        var assignment = new Data.ModeratorAssignment
        {
            Id = assignmentId,
            UserId = userId,
            Reason = "I want to help moderate",
            Status = ModeratorAssignmentStatus.PENDING,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };
        dbContext.ModeratorAssignments.Add(assignment);
        await dbContext.SaveChangesAsync();

        mockUserManager.Setup(um => um.IsInRoleAsync(user, "Moderator"))
            .ReturnsAsync(false);
        mockUserManager.Setup(um => um.AddToRoleAsync(user, "Moderator"))
            .ReturnsAsync(IdentityResult.Success);

        var dto = new UpdateModeratorAssignmentStatusDto(ModeratorAssignmentStatus.ACCEPTED, adminId);
        var request = new UpdateModeratorAssignmentStatusRequest(assignmentId, dto);

        var handler = new UpdateModeratorAssignmentStatusHandler(dbContext, mapper, mockUserManager.Object);
        var beforeUpdate = DateTime.UtcNow;

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var updatedAssignment = await dbContext.ModeratorAssignments.FindAsync(assignmentId);
        updatedAssignment.Should().NotBeNull();
        updatedAssignment!.ReviewedByAdminId.Should().Be(adminId);
        updatedAssignment.ReviewedDate.Should().NotBeNull();
        updatedAssignment.ReviewedDate!.Value.Should().BeCloseTo(beforeUpdate, TimeSpan.FromSeconds(5));
    }
}
