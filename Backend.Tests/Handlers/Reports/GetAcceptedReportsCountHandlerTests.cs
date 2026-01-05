using Backend.Data;
using Backend.Features.Reports.Enums;
using Backend.Features.Reports.GetAcceptedReportsCount;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using FluentAssertions;

namespace Backend.Tests.Handlers.Reports;

public class GetAcceptedReportsCountTests
{
    private static ApplicationContext CreateInMemoryDbContext(string guid)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: guid)
            .Options;

        var context = new ApplicationContext(options);
        return context;
    }
    
    [Fact]
    public async Task Given_AcceptedReportsWithinPeriod_When_GettingCount_Then_ReturnsCorrectCount()
    {
        // Arrange
        var context = CreateInMemoryDbContext("get-accepted-reports-count-" + Guid.NewGuid().ToString());
        var itemId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var ownerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        
        context.Reports.AddRange(
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId, 
                UserId = userId,
                OwnerId = ownerId,
                Description = "Report 1",
                Status = ReportStatus.ACCEPTED, 
                CreatedDate = DateTime.UtcNow.AddDays(-3)
            },
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId, 
                UserId = userId,
                OwnerId = ownerId,
                Description = "Report 2",
                Status = ReportStatus.DECLINED, 
                CreatedDate = DateTime.UtcNow.AddDays(-5)
            },
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId, 
                UserId = userId,
                OwnerId = ownerId,
                Description = "Report 3",
                Status = ReportStatus.ACCEPTED, 
                CreatedDate = DateTime.UtcNow.AddDays(-6)
            }
        );
        await context.SaveChangesAsync();
        
        var handler = new GetAcceptedReportsCountHandler(context);
        var request = new GetAcceptedReportsCountRequest(itemId, 7);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        valueResult.Value.Should().NotBeNull();
        
        var resultValue = valueResult.Value;
        var resultItemId = resultValue.GetType().GetProperty("itemId")?.GetValue(resultValue);
        var resultCount = resultValue.GetType().GetProperty("count")?.GetValue(resultValue);
        
        resultItemId.Should().Be(itemId);
        resultCount.Should().Be(2);
    }
    
    [Fact]
    public async Task Given_NoAcceptedReports_When_GettingCount_Then_ReturnsZero()
    {
        // Arrange
        var context = CreateInMemoryDbContext("get-accepted-reports-count-" + Guid.NewGuid().ToString());
        var itemId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var ownerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        
        context.Reports.AddRange(
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId, 
                UserId = userId,
                OwnerId = ownerId,
                Description = "Declined report",
                Status = ReportStatus.DECLINED, 
                CreatedDate = DateTime.UtcNow.AddDays(-3)
            },
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId, 
                UserId = userId,
                OwnerId = ownerId,
                Description = "Pending report",
                Status = ReportStatus.PENDING, 
                CreatedDate = DateTime.UtcNow.AddDays(-2)
            }
        );
        await context.SaveChangesAsync();
        
        var handler = new GetAcceptedReportsCountHandler(context);
        var request = new GetAcceptedReportsCountRequest(itemId, 7);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        valueResult.Value.Should().NotBeNull();
        
        var resultValue = valueResult.Value;
        var resultItemId = resultValue.GetType().GetProperty("itemId")?.GetValue(resultValue);
        var resultCount = resultValue.GetType().GetProperty("count")?.GetValue(resultValue);
        
        resultItemId.Should().Be(itemId);
        resultCount.Should().Be(0);
    }
    
    [Fact]
    public async Task Given_AcceptedReportsOutsidePeriod_When_GettingCount_Then_ReturnsZero()
    {
        // Arrange
        var context = CreateInMemoryDbContext("get-accepted-reports-count-" + Guid.NewGuid().ToString());
        var itemId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var ownerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        
        context.Reports.AddRange(
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId, 
                UserId = userId,
                OwnerId = ownerId,
                Description = "Old accepted report 1",
                Status = ReportStatus.ACCEPTED, 
                CreatedDate = DateTime.UtcNow.AddDays(-10)
            },
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId, 
                UserId = userId,
                OwnerId = ownerId,
                Description = "Old accepted report 2",
                Status = ReportStatus.ACCEPTED, 
                CreatedDate = DateTime.UtcNow.AddDays(-15)
            }
        );
        await context.SaveChangesAsync();
        
        var handler = new GetAcceptedReportsCountHandler(context);
        var request = new GetAcceptedReportsCountRequest(itemId, 7);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        valueResult.Value.Should().NotBeNull();
        
        var resultValue = valueResult.Value;
        var resultItemId = resultValue.GetType().GetProperty("itemId")?.GetValue(resultValue);
        var resultCount = resultValue.GetType().GetProperty("count")?.GetValue(resultValue);
        
        resultItemId.Should().Be(itemId);
        resultCount.Should().Be(0);
    }
    
    [Fact]
    public async Task Given_MixOfReportsInsideAndOutsidePeriod_When_GettingCount_Then_ReturnsOnlyInsidePeriod()
    {
        // Arrange
        var context = CreateInMemoryDbContext("get-accepted-reports-count-" + Guid.NewGuid().ToString());
        var itemId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var ownerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        
        context.Reports.AddRange(
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId, 
                UserId = userId,
                OwnerId = ownerId,
                Description = "Within period",
                Status = ReportStatus.ACCEPTED, 
                CreatedDate = DateTime.UtcNow.AddDays(-5)
            },
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId, 
                UserId = userId,
                OwnerId = ownerId,
                Description = "Outside period",
                Status = ReportStatus.ACCEPTED, 
                CreatedDate = DateTime.UtcNow.AddDays(-20)
            },
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId, 
                UserId = userId,
                OwnerId = ownerId,
                Description = "Within period 2",
                Status = ReportStatus.ACCEPTED, 
                CreatedDate = DateTime.UtcNow.AddDays(-10)
            }
        );
        await context.SaveChangesAsync();
        
        var handler = new GetAcceptedReportsCountHandler(context);
        var request = new GetAcceptedReportsCountRequest(itemId, 14);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        valueResult.Value.Should().NotBeNull();
        
        var resultValue = valueResult.Value;
        var resultItemId = resultValue.GetType().GetProperty("itemId")?.GetValue(resultValue);
        var resultCount = resultValue.GetType().GetProperty("count")?.GetValue(resultValue);
        
        resultItemId.Should().Be(itemId);
        resultCount.Should().Be(2);
    }
    
    [Fact]
    public async Task Given_MultipleItemsWithReports_When_GettingCountForSpecificItem_Then_ReturnsOnlyThatItemCount()
    {
        // Arrange
        var context = CreateInMemoryDbContext("get-accepted-reports-count-" + Guid.NewGuid().ToString());
        var itemId1 = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var itemId2 = Guid.Parse("87654321-4321-4321-4321-210987654321");
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var ownerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        
        context.Reports.AddRange(
            // Reports for itemId1
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId1, 
                UserId = userId,
                OwnerId = ownerId,
                Description = "Item 1 report 1",
                Status = ReportStatus.ACCEPTED, 
                CreatedDate = DateTime.UtcNow.AddDays(-3)
            },
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId1, 
                UserId = userId,
                OwnerId = ownerId,
                Description = "Item 1 report 2",
                Status = ReportStatus.ACCEPTED, 
                CreatedDate = DateTime.UtcNow.AddDays(-5)
            },
            // Reports for itemId2
            new Report 
            { 
                Id = Guid.NewGuid(), 
                ItemId = itemId2, 
                UserId = userId,
                OwnerId = ownerId,
                Description = "Item 2 report",
                Status = ReportStatus.ACCEPTED, 
                CreatedDate = DateTime.UtcNow.AddDays(-4)
            }
        );
        await context.SaveChangesAsync();
        
        var handler = new GetAcceptedReportsCountHandler(context);
        var request = new GetAcceptedReportsCountRequest(itemId1, 7);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        valueResult.Value.Should().NotBeNull();
        
        var resultValue = valueResult.Value;
        var resultItemId = resultValue.GetType().GetProperty("itemId")?.GetValue(resultValue);
        var resultCount = resultValue.GetType().GetProperty("count")?.GetValue(resultValue);
        
        resultItemId.Should().Be(itemId1);
        resultCount.Should().Be(2);
    }
    
    [Fact]
    public async Task Given_NoReportsInDatabase_When_GettingCount_Then_ReturnsZero()
    {
        // Arrange
        var context = CreateInMemoryDbContext("get-accepted-reports-count-" + Guid.NewGuid().ToString());
        var itemId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        
        var handler = new GetAcceptedReportsCountHandler(context);
        var request = new GetAcceptedReportsCountRequest(itemId, 7);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        valueResult.Value.Should().NotBeNull();
        
        var resultValue = valueResult.Value;
        var resultItemId = resultValue.GetType().GetProperty("itemId")?.GetValue(resultValue);
        var resultCount = resultValue.GetType().GetProperty("count")?.GetValue(resultValue);
        
        resultItemId.Should().Be(itemId);
        resultCount.Should().Be(0);
    }

}