using AutoMapper;
using Backend.Data;
using Backend.Features.Reports.DTO;
using Backend.Features.Reports.Enums;
using Backend.Features.Reports.GetReportsByItem;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Tests.Handlers.Reports;

public class GetReportsByItemHandlerTests
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
    public async Task Given_ItemWithReports_When_Handle_Then_ReturnsReportsForThatItem()
    {
        // Arrange
        var context = CreateInMemoryDbContext("get-reports-by-item-handler-" + Guid.NewGuid().ToString());
        var itemId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var otherItemId = Guid.Parse("87654321-4321-4321-4321-210987654321");
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var ownerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        
        context.Reports.AddRange(
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId, 
                UserId = userId,
                OwnerId = ownerId,
                Description = "Report 1 for item",
                Status = ReportStatus.PENDING, 
                CreatedDate = DateTime.UtcNow.AddDays(-1)
            },
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId, 
                UserId = userId,
                OwnerId = ownerId,
                Description = "Report 2 for item",
                Status = ReportStatus.ACCEPTED, 
                CreatedDate = DateTime.UtcNow.AddDays(-2)
            },
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = otherItemId, 
                UserId = userId,
                OwnerId = ownerId,
                Description = "Report for other item",
                Status = ReportStatus.PENDING, 
                CreatedDate = DateTime.UtcNow.AddDays(-3)
            }
        );
        await context.SaveChangesAsync();
        
        var mapper = CreateMapper();
        var handler = new GetReportsByItemHandler(context, mapper);
        var request = new GetReportsByItemRequest(itemId);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result);
        var valueProperty = result.GetType().GetProperty("Value");
        var reportsList = valueProperty?.GetValue(result) as List<ReportDto>;
        Assert.NotNull(reportsList);
        Assert.Equal(2, reportsList.Count);
        Assert.All(reportsList, r => Assert.Equal(itemId, r.ItemId));
    }
    
    [Fact]
    public async Task Given_ItemWithNoReports_When_Handle_Then_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateInMemoryDbContext("get-reports-by-item-handler-" + Guid.NewGuid().ToString());
        var itemId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        
        var mapper = CreateMapper();
        var handler = new GetReportsByItemHandler(context, mapper);
        var request = new GetReportsByItemRequest(itemId);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result);
        var valueProperty = result.GetType().GetProperty("Value");
        var reportsList = valueProperty?.GetValue(result) as List<ReportDto>;
        Assert.NotNull(reportsList);
        Assert.Empty(reportsList);
    }
    
}
