using AutoMapper;
using Backend.Data;
using Backend.Features.Reports.CreateReport;
using Backend.Features.Reports.DTO;
using Backend.Features.Reports.Enums;
using Backend.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Tests.Handlers.Reports;

public class CreateReportHandlerTests
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
            cfg.CreateMap<CreateReportDto, Report>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OwnerId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.ModeratorId, opt => opt.Ignore());
            cfg.CreateMap<Report, ReportDto>();
        }, new LoggerFactory());

        return config.CreateMapper();
    }
    
    [Fact]
    public async Task Given_ValidReportDto_When_Handle_Then_ReturnsCreatedWithReport()
    {
        // Arrange
        var context = CreateInMemoryDbContext("create-report-handler-" + Guid.NewGuid().ToString());
        var userId = Guid.Parse("66666666-7777-8888-9999-000000000000");
        var itemId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        var reportDto = new CreateReportDto(
            itemId,
            userId, 
            "This is a test report"
        );

        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Test Item",
            Description = "A test item",
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };

        var user = new User
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            UserName = "testuser",
            Email = "test@test.com"
        };

        await context.Items.AddAsync(item);
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var mapper = CreateMapper();
        var request = new CreateReportRequest(reportDto);
        var handler = new CreateReportHandler(context, mapper);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var createdResult = result.GetType().GetProperty("StatusCode")?.GetValue(result);
        Assert.Equal(201, createdResult);
        
        // Verify report was saved to database
        var savedReport = await context.Reports.FirstOrDefaultAsync();
        Assert.NotNull(savedReport);
        Assert.Equal(itemId, savedReport.ItemId);
        Assert.Equal(userId, savedReport.UserId);
        Assert.Equal(ownerId, savedReport.OwnerId);
        Assert.Equal("This is a test report", savedReport.Description);
        Assert.Equal(ReportStatus.PENDING, savedReport.Status);
    }
    
    [Fact]
    public async Task Given_NonExistentItem_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext("create-report-handler-" + Guid.NewGuid().ToString());
        var userId = Guid.Parse("66666666-7777-8888-9999-000000000000");
        var itemId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        
        var reportDto = new CreateReportDto(
            itemId,
            userId, 
            "This is a test report"
        );

        var user = new User
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            UserName = "testuser",
            Email = "test@test.com"
        };

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var mapper = CreateMapper();
        var request = new CreateReportRequest(reportDto);
        var handler = new CreateReportHandler(context, mapper);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var statusCode = result.GetType().GetProperty("StatusCode")?.GetValue(result);
        Assert.Equal(404, statusCode);
    }
    
    [Fact]
    public async Task Given_NonExistentUser_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext("create-report-handler-" + Guid.NewGuid().ToString());
        var userId = Guid.Parse("66666666-7777-8888-9999-000000000000");
        var itemId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        var reportDto = new CreateReportDto(
            itemId,
            userId, 
            "This is a test report"
        );

        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Test Item",
            Description = "A test item",
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };

        await context.Items.AddAsync(item);
        await context.SaveChangesAsync();

        var mapper = CreateMapper();
        var request = new CreateReportRequest(reportDto);
        var handler = new CreateReportHandler(context, mapper);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var statusCode = result.GetType().GetProperty("StatusCode")?.GetValue(result);
        Assert.Equal(404, statusCode);
    }
    
    [Fact]
    public async Task Given_ValidReport_When_ModeratorExists_Then_AssignsModeratorWithLeastPendingReports()
    {
        // Arrange
        var context = CreateInMemoryDbContext("create-report-handler-" + Guid.NewGuid().ToString());
        var userId = Guid.Parse("66666666-7777-8888-9999-000000000000");
        var itemId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var moderator1Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var moderator2Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var moderatorRoleId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
        // Create moderator role
        var moderatorRole = new IdentityRole<Guid>
        {
            Id = moderatorRoleId,
            Name = "Moderator",
            NormalizedName = "MODERATOR"
        };
        await context.Roles.AddAsync(moderatorRole);
        
        // Create moderators
        var moderator1 = new User { Id = moderator1Id, UserName = "mod1", Email = "mod1@test.com" };
        var moderator2 = new User { Id = moderator2Id, UserName = "mod2", Email = "mod2@test.com" };
        await context.Users.AddRangeAsync(moderator1, moderator2);
        
        // Assign moderator role
        await context.UserRoles.AddRangeAsync(
            new IdentityUserRole<Guid> { UserId = moderator1Id, RoleId = moderatorRoleId },
            new IdentityUserRole<Guid> { UserId = moderator2Id, RoleId = moderatorRoleId }
        );
        
        // Create existing pending report for moderator1
        var existingReport = new Report
        {
            Id = Guid.NewGuid(),
            ItemId = itemId,
            UserId = userId,
            OwnerId = ownerId,
            ModeratorId = moderator1Id,
            Description = "Existing report",
            Status = ReportStatus.PENDING,
            CreatedDate = DateTime.UtcNow
        };
        await context.Reports.AddAsync(existingReport);
        
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Test Item",
            Description = "A test item",
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };
        
        var user = new User { Id = userId, UserName = "testuser", Email = "test@test.com" };
        
        await context.Items.AddAsync(item);
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var reportDto = new CreateReportDto(itemId, userId, "New test report");
        var mapper = CreateMapper();
        var request = new CreateReportRequest(reportDto);
        var handler = new CreateReportHandler(context, mapper);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        var newReport = await context.Reports
            .Where(r => r.Description == "New test report")
            .FirstOrDefaultAsync();
        Assert.NotNull(newReport);
        Assert.Equal(moderator2Id, newReport.ModeratorId); // Should assign to moderator2 who has fewer pending reports
    }
    
    [Fact]
    public async Task Given_ValidReport_When_NoModerator_Then_AssignsAdmin()
    {
        // Arrange
        var context = CreateInMemoryDbContext("create-report-handler-" + Guid.NewGuid().ToString());
        var userId = Guid.Parse("66666666-7777-8888-9999-000000000000");
        var itemId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var adminId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var adminRoleId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
        // Create admin role
        var adminRole = new IdentityRole<Guid>
        {
            Id = adminRoleId,
            Name = "Admin",
            NormalizedName = "ADMIN"
        };
        await context.Roles.AddAsync(adminRole);
        
        // Create admin user
        var admin = new User { Id = adminId, UserName = "admin", Email = "admin@test.com" };
        await context.Users.AddAsync(admin);
        
        // Assign admin role
        await context.UserRoles.AddAsync(
            new IdentityUserRole<Guid> { UserId = adminId, RoleId = adminRoleId }
        );
        
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Test Item",
            Description = "A test item",
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };
        
        var user = new User { Id = userId, UserName = "testuser", Email = "test@test.com" };
        
        await context.Items.AddAsync(item);
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var reportDto = new CreateReportDto(itemId, userId, "Test report");
        var mapper = CreateMapper();
        var request = new CreateReportRequest(reportDto);
        var handler = new CreateReportHandler(context, mapper);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        var newReport = await context.Reports.FirstOrDefaultAsync();
        Assert.NotNull(newReport);
        Assert.Equal(adminId, newReport.ModeratorId); // Should assign to admin when no moderator exists
    }
    
    [Fact]
    public async Task Given_ValidReport_When_NoModeratorOrAdmin_Then_CreatesWithNullModerator()
    {
        // Arrange
        var context = CreateInMemoryDbContext("create-report-handler-" + Guid.NewGuid().ToString());
        var userId = Guid.Parse("66666666-7777-8888-9999-000000000000");
        var itemId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Test Item",
            Description = "A test item",
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };
        
        var user = new User { Id = userId, UserName = "testuser", Email = "test@test.com" };
        
        await context.Items.AddAsync(item);
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var reportDto = new CreateReportDto(itemId, userId, "Test report");
        var mapper = CreateMapper();
        var request = new CreateReportRequest(reportDto);
        var handler = new CreateReportHandler(context, mapper);

        // Act
         await handler.Handle(request, CancellationToken.None);

        // Assert
        var newReport = await context.Reports.FirstOrDefaultAsync();
        Assert.NotNull(newReport);
        Assert.Null(newReport.ModeratorId); // Should be null when no moderator or admin exists
    }
}