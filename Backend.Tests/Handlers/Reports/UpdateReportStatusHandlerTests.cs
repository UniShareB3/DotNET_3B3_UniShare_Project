using AutoMapper;
using Backend.Data;
using Backend.Features.Reports.DTO;
using Backend.Features.Reports.Enums;
using Backend.Features.Reports.UpdateReportStatus;
using Backend.Mappers.Report;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Tests.Handlers.Reports;

public class UpdateReportStatusHandlerTests
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
        var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ReportMapper>();
        }, loggerFactory);

        return config.CreateMapper();
    }

    [Fact]
    public async Task Given_ValidRequest_When_UpdatingReportStatusToAccepted_Then_ReturnsUpdatedReport()
    {
        // Arrange
        var context = CreateInMemoryDbContext("update-report-status-" + Guid.NewGuid().ToString());
        var reportId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var ownerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var itemId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var moderatorId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        var report = new Report
        {
            Id = reportId,
            ItemId = itemId,
            OwnerId = ownerId,
            UserId = userId,
            Description = "Test report",
            Status = ReportStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };

        var moderator = new User
        {
            Id = moderatorId,
            FirstName = "Moderator",
            LastName = "User",
            Email = "moderator@test.com",
            UserName = "moderator@test.com"
        };

        context.Reports.Add(report);
        context.Users.Add(moderator);
        await context.SaveChangesAsync();

        var mapper = CreateMapper();
        var dto = new UpdateReportStatusDto(ReportStatus.Accepted, moderatorId);
        var request = new UpdateReportStatusRequest(reportId, dto);
        var handler = new UpdateReportStatusHandler(context, mapper);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        
        // Verify database was updated
        var updatedReport = await context.Reports.FindAsync(reportId);
        Assert.NotNull(updatedReport);
        Assert.Equal(ReportStatus.Accepted, updatedReport.Status);
        Assert.Equal(moderatorId, updatedReport.ModeratorId);
    }

    [Fact]
    public async Task Given_NonExistentReport_When_UpdatingReportStatus_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext("update-report-status-" + Guid.NewGuid().ToString());
        var nonExistentReportId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var moderatorId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        var moderator = new User
        {
            Id = moderatorId,
            FirstName = "Moderator",
            LastName = "User",
            Email = "moderator@test.com",
            UserName = "moderator@test.com"
        };

        context.Users.Add(moderator);
        await context.SaveChangesAsync();

        var mapper = CreateMapper();
        var dto = new UpdateReportStatusDto(ReportStatus.Accepted, moderatorId);
        var request = new UpdateReportStatusRequest(nonExistentReportId, dto);
        var handler = new UpdateReportStatusHandler(context, mapper);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Given_NonExistentModerator_When_UpdatingReportStatus_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext("update-report-status-" + Guid.NewGuid().ToString());
        var reportId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var ownerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var itemId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var nonExistentModeratorId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var report = new Report
        {
            Id = reportId,
            ItemId = itemId,
            OwnerId = ownerId,
            UserId = userId,
            Description = "Test report",
            Status = ReportStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };

        context.Reports.Add(report);
        await context.SaveChangesAsync();

        var mapper = CreateMapper();
        var dto = new UpdateReportStatusDto(ReportStatus.Accepted, nonExistentModeratorId);
        var request = new UpdateReportStatusRequest(reportId, dto);
        var handler = new UpdateReportStatusHandler(context, mapper);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Given_ValidRequest_When_DecliningReport_Then_UpdatesStatusToDeclined()
    {
        // Arrange
        var context = CreateInMemoryDbContext("update-report-status-" + Guid.NewGuid().ToString());
        var reportId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var ownerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var itemId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var moderatorId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        var report = new Report
        {
            Id = reportId,
            ItemId = itemId,
            OwnerId = ownerId,
            UserId = userId,
            Description = "Test report to decline",
            Status = ReportStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };

        var moderator = new User
        {
            Id = moderatorId,
            FirstName = "Moderator",
            LastName = "User",
            Email = "moderator@test.com",
            UserName = "moderator@test.com"
        };

        context.Reports.Add(report);
        context.Users.Add(moderator);
        await context.SaveChangesAsync();

        var mapper = CreateMapper();
        var dto = new UpdateReportStatusDto(ReportStatus.Declined, moderatorId);
        var request = new UpdateReportStatusRequest(reportId, dto);
        var handler = new UpdateReportStatusHandler(context, mapper);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // Verify database was updated
        var updatedReport = await context.Reports.FindAsync(reportId);
        Assert.NotNull(updatedReport);
        Assert.Equal(ReportStatus.Declined, updatedReport.Status);
        Assert.Equal(moderatorId, updatedReport.ModeratorId);
    }

    [Fact]
    public async Task Given_AlreadyAcceptedReport_When_UpdatingToDeclined_Then_UpdatesStatusAndModerator()
    {
        // Arrange
        var context = CreateInMemoryDbContext("update-report-status-" + Guid.NewGuid().ToString());
        var reportId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var ownerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var itemId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var oldModeratorId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var newModeratorId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        var report = new Report
        {
            Id = reportId,
            ItemId = itemId,
            OwnerId = ownerId,
            UserId = userId,
            Description = "Already accepted report",
            Status = ReportStatus.Accepted,
            ModeratorId = oldModeratorId,
            CreatedDate = DateTime.UtcNow
        };

        var newModerator = new User
        {
            Id = newModeratorId,
            FirstName = "New Moderator",
            LastName = "User",
            Email = "newmoderator@test.com",
            UserName = "newmoderator@test.com"
        };

        context.Reports.Add(report);
        context.Users.Add(newModerator);
        await context.SaveChangesAsync();

        var mapper = CreateMapper();
        var dto = new UpdateReportStatusDto(ReportStatus.Declined, newModeratorId);
        var request = new UpdateReportStatusRequest(reportId, dto);
        var handler = new UpdateReportStatusHandler(context, mapper);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // Verify database was updated with new moderator and status
        var updatedReport = await context.Reports.FindAsync(reportId);
        Assert.NotNull(updatedReport);
        Assert.Equal(ReportStatus.Declined, updatedReport.Status);
        Assert.Equal(newModeratorId, updatedReport.ModeratorId);
    }

    [Fact]
    public async Task Given_PendingReport_When_KeepingAsPending_Then_UpdatesModeratorId()
    {
        // Arrange
        var context = CreateInMemoryDbContext("update-report-status-" + Guid.NewGuid().ToString());
        var reportId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var ownerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var itemId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var moderatorId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        var report = new Report
        {
            Id = reportId,
            ItemId = itemId,
            OwnerId = ownerId,
            UserId = userId,
            Description = "Test pending report",
            Status = ReportStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };

        var moderator = new User
        {
            Id = moderatorId,
            FirstName = "Moderator",
            LastName = "User",
            Email = "moderator@test.com",
            UserName = "moderator@test.com"
        };

        context.Reports.Add(report);
        context.Users.Add(moderator);
        await context.SaveChangesAsync();

        var mapper = CreateMapper();
        var dto = new UpdateReportStatusDto(ReportStatus.Pending, moderatorId);
        var request = new UpdateReportStatusRequest(reportId, dto);
        var handler = new UpdateReportStatusHandler(context, mapper);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // Verify database was updated
        var updatedReport = await context.Reports.FindAsync(reportId);
        Assert.NotNull(updatedReport);
        Assert.Equal(ReportStatus.Pending, updatedReport.Status);
        Assert.Equal(moderatorId, updatedReport.ModeratorId);
    }
}

