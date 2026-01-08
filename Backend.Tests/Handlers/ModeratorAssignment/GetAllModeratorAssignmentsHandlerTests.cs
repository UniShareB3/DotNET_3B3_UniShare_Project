using AutoMapper;
using Backend.Features.ModeratorAssignment.DTO;
using Backend.Features.ModeratorAssignment.Enums;
using Backend.Features.ModeratorAssignment.GetAllModeratorAssignments;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Tests.Handlers.ModeratorAssignment;

public class GetAllModeratorAssignmentsHandlerTests
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
            cfg.CreateMap<Data.ModeratorAssignment, ModeratorAssignmentDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        }, NullLoggerFactory.Instance);
        return config.CreateMapper();
    }

    [Fact]
    public async Task Given_NoAssignments_When_GetAllModeratorAssignments_Then_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();
        var handler = new GetAllModeratorAssignmentsHandler(context, mapper);
        var request = new GetAllModeratorAssignmentsRequest();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as Ok<List<ModeratorAssignmentDto>>;
        okResult.Should().NotBeNull();
        okResult.Value.Should().NotBeNull();
        okResult.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_MultipleAssignments_When_GetAllModeratorAssignments_Then_ReturnsAllAssignmentsOrderedByCreatedDateDescending()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();

        var userId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var userId3 = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var assignment1 = new Data.ModeratorAssignment
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            UserId = userId1,
            Reason = "First assignment",
            Status = ModeratorAssignmentStatus.Pending,
            CreatedDate = DateTime.UtcNow.AddDays(-2)
        };

        var assignment2 = new Data.ModeratorAssignment
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            UserId = userId2,
            Reason = "Second assignment",
            Status = ModeratorAssignmentStatus.Accepted,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };

        var assignment3 = new Data.ModeratorAssignment
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            UserId = userId3,
            Reason = "Third assignment",
            Status = ModeratorAssignmentStatus.Rejected,
            CreatedDate = DateTime.UtcNow
        };

        context.ModeratorAssignments.AddRange(assignment1, assignment2, assignment3);
        await context.SaveChangesAsync();

        var handler = new GetAllModeratorAssignmentsHandler(context, mapper);
        var request = new GetAllModeratorAssignmentsRequest();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as Ok<List<ModeratorAssignmentDto>>;
        okResult.Should().NotBeNull();
        okResult.Value.Should().NotBeNull();
        okResult.Value.Should().HaveCount(3);

        // Verify ordering by CreatedDate descending (newest first)
        okResult.Value[0].Id.Should().Be(assignment3.Id);
        okResult.Value[1].Id.Should().Be(assignment2.Id);
        okResult.Value[2].Id.Should().Be(assignment1.Id);

        // Verify content
        okResult.Value[0].Reason.Should().Be("Third assignment");
        okResult.Value[0].Status.Should().Be("Rejected");
        okResult.Value[1].Reason.Should().Be("Second assignment");
        okResult.Value[1].Status.Should().Be("Accepted");
        okResult.Value[2].Reason.Should().Be("First assignment");
        okResult.Value[2].Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Given_SingleAssignment_When_GetAllModeratorAssignments_Then_ReturnsSingleAssignment()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();

        var userId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var assignmentId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        var assignment = new Data.ModeratorAssignment
        {
            Id = assignmentId,
            UserId = userId,
            Reason = "Single assignment",
            Status = ModeratorAssignmentStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };

        context.ModeratorAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var handler = new GetAllModeratorAssignmentsHandler(context, mapper);
        var request = new GetAllModeratorAssignmentsRequest();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as Ok<List<ModeratorAssignmentDto>>;
        okResult.Should().NotBeNull();
        okResult.Value.Should().NotBeNull();
        okResult.Value.Should().HaveCount(1);
        okResult.Value[0].Id.Should().Be(assignmentId);
        okResult.Value[0].UserId.Should().Be(userId);
        okResult.Value[0].Reason.Should().Be("Single assignment");
    }

    [Fact]
    public async Task Given_AssignmentsWithDifferentStatuses_When_GetAllModeratorAssignments_Then_ReturnsAllStatuses()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();

        var pendingAssignment = new Data.ModeratorAssignment
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Reason = "Pending assignment",
            Status = ModeratorAssignmentStatus.Pending,
            CreatedDate = DateTime.UtcNow.AddHours(-3)
        };

        var acceptedAssignment = new Data.ModeratorAssignment
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Reason = "Accepted assignment",
            Status = ModeratorAssignmentStatus.Accepted,
            CreatedDate = DateTime.UtcNow.AddHours(-2),
            ReviewedByAdminId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            ReviewedDate = DateTime.UtcNow.AddHours(-2)
        };

        var rejectedAssignment = new Data.ModeratorAssignment
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Reason = "Rejected assignment",
            Status = ModeratorAssignmentStatus.Rejected,
            CreatedDate = DateTime.UtcNow.AddHours(-1),
            ReviewedByAdminId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            ReviewedDate = DateTime.UtcNow.AddHours(-1)
        };

        context.ModeratorAssignments.AddRange(pendingAssignment, acceptedAssignment, rejectedAssignment);
        await context.SaveChangesAsync();

        var handler = new GetAllModeratorAssignmentsHandler(context, mapper);
        var request = new GetAllModeratorAssignmentsRequest();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as Ok<List<ModeratorAssignmentDto>>;
        okResult.Should().NotBeNull();
        okResult.Value.Should().HaveCount(3);

        var statuses = okResult.Value.Select(a => a.Status).ToList();
        statuses.Should().Contain("Pending");
        statuses.Should().Contain("Accepted");
        statuses.Should().Contain("Rejected");
    }

    [Fact]
    public async Task Given_AssignmentsWithReviewDetails_When_GetAllModeratorAssignments_Then_ReturnsReviewDetails()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();

        var adminId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var reviewedDate = DateTime.UtcNow.AddDays(-1);

        var reviewedAssignment = new Data.ModeratorAssignment
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Reason = "Reviewed assignment",
            Status = ModeratorAssignmentStatus.Accepted,
            CreatedDate = DateTime.UtcNow.AddDays(-2),
            ReviewedByAdminId = adminId,
            ReviewedDate = reviewedDate
        };

        context.ModeratorAssignments.Add(reviewedAssignment);
        await context.SaveChangesAsync();

        var handler = new GetAllModeratorAssignmentsHandler(context, mapper);
        var request = new GetAllModeratorAssignmentsRequest();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as Ok<List<ModeratorAssignmentDto>>;
        okResult.Should().NotBeNull();
        okResult.Value.Should().HaveCount(1);
        okResult.Value[0].ReviewedByAdminId.Should().Be(adminId);
        okResult.Value[0].ReviewedDate.Should().NotBeNull();
        okResult.Value[0].ReviewedDate.Should().BeCloseTo(reviewedDate, TimeSpan.FromSeconds(1));
    }
}

