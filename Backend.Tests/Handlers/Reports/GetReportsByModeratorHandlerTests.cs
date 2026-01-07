using AutoMapper;
using Backend.Data;
using Backend.Features.Reports.DTO;
using Backend.Features.Reports.Enums;
using Backend.Features.Reports.GetReportsByModerator;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Tests.Handlers.Reports;

public class GetReportsByModeratorHandlerTests
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
            cfg.CreateMap<Report, ReportDto>();
        }, new LoggerFactory());

        return config.CreateMapper();
    }
    
    [Fact]
    public async Task Given_ModeratorWithReports_When_Handle_Then_ReturnsReportsForThatModerator()
    {
        // Arrange
        var context = CreateInMemoryDbContext("get-reports-by-moderator-handler-" + Guid.NewGuid().ToString());
        var moderatorId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var otherModeratorId = Guid.Parse("87654321-4321-4321-4321-210987654321");
        var itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var ownerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
        context.Reports.AddRange(
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId, 
                UserId = userId,
                OwnerId = ownerId,
                ModeratorId = moderatorId,
                Description = "Report 1 for moderator",
                Status = ReportStatus.Pending, 
                CreatedDate = DateTime.UtcNow.AddDays(-1)
            },
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId, 
                UserId = userId,
                OwnerId = ownerId,
                ModeratorId = moderatorId,
                Description = "Report 2 for moderator",
                Status = ReportStatus.Accepted, 
                CreatedDate = DateTime.UtcNow.AddDays(-2)
            },
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId, 
                UserId = userId,
                OwnerId = ownerId,
                ModeratorId = otherModeratorId,
                Description = "Report for other moderator",
                Status = ReportStatus.Pending, 
                CreatedDate = DateTime.UtcNow.AddDays(-3)
            }
        );
        await context.SaveChangesAsync();
        
        var mapper = CreateMapper();
        var handler = new GetReportsByModeratorHandler(context, mapper);
        var request = new GetReportsByModeratorRequest(moderatorId);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result);
        var valueProperty = result.GetType().GetProperty("Value");
        var reportsList = valueProperty?.GetValue(result) as List<ReportDto>;
        Assert.NotNull(reportsList);
        Assert.Equal(2, reportsList.Count);
        Assert.All(reportsList, r => Assert.Equal(moderatorId, r.ModeratorId));
    }
    
    [Fact]
    public async Task Given_ModeratorWithNoReports_When_Handle_Then_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateInMemoryDbContext("get-reports-by-moderator-handler-" + Guid.NewGuid().ToString());
        var moderatorId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        
        var mapper = CreateMapper();
        var handler = new GetReportsByModeratorHandler(context, mapper);
        var request = new GetReportsByModeratorRequest(moderatorId);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result);
        var valueProperty = result.GetType().GetProperty("Value");
        var reportsList = valueProperty?.GetValue(result) as List<ReportDto>;
        Assert.NotNull(reportsList);
        Assert.Empty(reportsList);
    }
    
    [Fact]
    public async Task Given_ReportsWithNullModerator_When_Handle_Then_DoesNotReturnThoseReports()
    {
        // Arrange
        var context = CreateInMemoryDbContext("get-reports-by-moderator-handler-" + Guid.NewGuid().ToString());
        var moderatorId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var ownerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
        context.Reports.AddRange(
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId, 
                UserId = userId,
                OwnerId = ownerId,
                ModeratorId = moderatorId,
                Description = "Report with moderator",
                Status = ReportStatus.Pending, 
                CreatedDate = DateTime.UtcNow.AddDays(-1)
            },
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId, 
                UserId = userId,
                OwnerId = ownerId,
                ModeratorId = null,
                Description = "Report without moderator",
                Status = ReportStatus.Pending, 
                CreatedDate = DateTime.UtcNow.AddDays(-2)
            }
        );
        await context.SaveChangesAsync();
        
        var mapper = CreateMapper();
        var handler = new GetReportsByModeratorHandler(context, mapper);
        var request = new GetReportsByModeratorRequest(moderatorId);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        var valueProperty = result.GetType().GetProperty("Value");
        var reportsList = valueProperty?.GetValue(result) as List<ReportDto>;
        Assert.NotNull(reportsList);
        Assert.Single(reportsList);
        Assert.Equal("Report with moderator", reportsList[0].Description);
    }
}
