using Backend.Data;
using Backend.Features.Reports.DTO;
using Backend.Features.Reports.Enums;
using Backend.Persistence;
using Backend.Validators;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Validators;

public class CreateReportDtoValidatorTests
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
    public async Task Given_AcceptedReportWithin30Days_When_ValidatingDto_Then_ReturnsValidationError()
    {
        // Arrange
        var itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var ownerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
        var dbContext = CreateInMemoryDbContext("accepted-report-" + Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        
        // Add an accepted report within the last 30 days
        var existingReport = new Report
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            ItemId = itemId,
            UserId = userId,
            OwnerId = ownerId,
            Description = "Previously accepted report",
            Status = ReportStatus.Accepted,
            CreatedDate = DateTime.UtcNow.AddDays(-15)
        };
        
        dbContext.Reports.Add(existingReport);
        await dbContext.SaveChangesAsync();
        
        var validator = new CreateReportDtoValidator(dbContext);
        var dto = new CreateReportDto(itemId, userId, "New report for same item");
        
        // Act
        var result = await validator.TestValidateAsync(dto);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("You already have an accepted report for this item in the last 30");
    }

    [Fact]
    public async Task Given_MoreThan5DeclinedReportsWithin30Days_When_ValidatingDto_Then_ReturnsValidationError()
    {
        // Arrange
        var itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var ownerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
        var dbContext = CreateInMemoryDbContext("declined-reports-" + Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        
        // Add 6 declined reports within the last 30 days (exceeding the limit of 5)
        for (int i = 0; i < 6; i++)
        {
            var declinedReport = new Report
            {
                Id = Guid.Parse($"44444444-4444-4444-4444-44444444444{i}"),
                ItemId = itemId,
                UserId = userId,
                OwnerId = ownerId,
                Description = $"Declined report number {i + 1}",
                Status = ReportStatus.Declined,
                CreatedDate = DateTime.UtcNow.AddDays(-i * 3)
            };
            
            dbContext.Reports.Add(declinedReport);
        }
        
        await dbContext.SaveChangesAsync();
        
        var validator = new CreateReportDtoValidator(dbContext);
        var dto = new CreateReportDto(itemId, userId, "Another report attempt");
        
        // Act
        var result = await validator.TestValidateAsync(dto);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("You have exceeded the number of declined reports for this item in the last");
    }
}

