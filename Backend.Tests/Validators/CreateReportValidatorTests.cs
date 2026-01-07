using Backend.Data;
using Backend.Features.Reports.CreateReport;
using Backend.Features.Reports.DTO;
using Backend.Features.Reports.Enums;
using Backend.Persistence;
using Backend.Validators;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Validators;

public class CreateReportValidatorTests
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
    public async Task Given_ValidRequest_When_NoExistingReport_Then_ReturnsValid()
    {
        // Arrange
        Guid itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        
        var dto = new CreateReportDto(itemId, userId, "This item violates community guidelines");
        var request = new CreateReportRequest(dto);
        var dbContext = CreateInMemoryDbContext("create-report-" + Guid.NewGuid());
        
        var dtoValidator = new CreateReportDtoValidator(dbContext);
        var validator = new CreateReportValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_Request_When_ItemIdIsEmpty_Then_ReturnsValidationError()
    {
        // Arrange
        Guid userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        
        var dto = new CreateReportDto(Guid.Empty, userId, "This item violates community guidelines");
        var request = new CreateReportRequest(dto);
        var dbContext = CreateInMemoryDbContext("create-report-" + Guid.NewGuid());
        
        var dtoValidator = new CreateReportDtoValidator(dbContext);
        var validator = new CreateReportValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Item ID is required");
    }

    [Fact]
    public async Task Given_Request_When_UserIdIsEmpty_Then_ReturnsValidationError()
    {
        // Arrange
        Guid itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        var dto = new CreateReportDto(itemId, Guid.Empty, "This item violates community guidelines");
        var request = new CreateReportRequest(dto);
        var dbContext = CreateInMemoryDbContext("create-report-" + Guid.NewGuid());
        
        var dtoValidator = new CreateReportDtoValidator(dbContext);
        var validator = new CreateReportValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "User ID is required");
    }

    [Fact]
    public async Task Given_Request_When_DescriptionIsEmpty_Then_ReturnsValidationError()
    {
        // Arrange
        Guid itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        
        var dto = new CreateReportDto(itemId, userId, "");
        var request = new CreateReportRequest(dto);
        var dbContext = CreateInMemoryDbContext("create-report-" + Guid.NewGuid());
        
        var dtoValidator = new CreateReportDtoValidator(dbContext);
        var validator = new CreateReportValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Description is required");
    }

    [Fact]
    public async Task Given_Request_When_DescriptionExceedsMaxLength_Then_ReturnsValidationError()
    {
        // Arrange
        Guid itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        string longDescription = new string('A', 1001); // 1001 characters
        
        var dto = new CreateReportDto(itemId, userId, longDescription);
        var request = new CreateReportRequest(dto);
        var dbContext = CreateInMemoryDbContext("create-report-" + Guid.NewGuid());
        
        var dtoValidator = new CreateReportDtoValidator(dbContext);
        var validator = new CreateReportValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Description cannot exceed 1000 characters");
    }

    [Fact]
    public async Task Given_Request_When_ItemAlreadyReportedByUserAsPending_Then_ReturnsValidationError()
    {
        // Arrange
        Guid itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid ownerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
        var dto = new CreateReportDto(itemId, userId, "This item violates community guidelines");
        var request = new CreateReportRequest(dto);
        var dbContext = CreateInMemoryDbContext("create-report-" + Guid.NewGuid());
        
        // Add existing Pending the report
        var existingReport = new Report
        {
            Id = Guid.NewGuid(),
            ItemId = itemId,
            UserId = userId,
            OwnerId = ownerId,
            Description = "Previous report",
            Status = ReportStatus.Pending,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };
        
        dbContext.Reports.Add(existingReport);
        await dbContext.SaveChangesAsync();
        
        var dtoValidator = new CreateReportDtoValidator(dbContext);
        var validator = new CreateReportValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "A month must pass between submitting a moderator assignment again");
    }

    [Fact]
    public async Task Given_Request_When_UserHasDeclinedReportForSameItemWithinMonth_Then_ReturnsValid()
    {
        // Arrange
        Guid itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid ownerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
        var dto = new CreateReportDto(itemId, userId, "This item violates community guidelines");
        var request = new CreateReportRequest(dto);
        var dbContext = CreateInMemoryDbContext("create-report-" + Guid.NewGuid());
        
        // Add 3 Declined reports within the last 30 days
        for (int i = 0; i < 3; i++)
        {
            var report = new Report
            {
                Id = Guid.NewGuid(),
                ItemId = itemId,
                UserId = userId,
                OwnerId = ownerId,
                Description = $"Declined report {i}",
                Status = ReportStatus.Declined,
                CreatedDate = DateTime.UtcNow.AddDays(-i * 5)
            };
            dbContext.Reports.Add(report);
        }
        
        await dbContext.SaveChangesAsync();
        
        var dtoValidator = new CreateReportDtoValidator(dbContext);
        var validator = new CreateReportValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_Request_When_UserHasMoreThan5DeclinedReportsForSameItemWithinMonth_Then_ReturnsValidationError()
    {
        // Arrange
        Guid itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid ownerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
        var dto = new CreateReportDto(itemId, userId, "This item violates community guidelines");
        var request = new CreateReportRequest(dto);
        var dbContext = CreateInMemoryDbContext("create-report-" + Guid.NewGuid());
        
        // Add MORE than 5 Declined reports within the last 30 days (6 reports)
        for (int i = 0; i < 6; i++)
        {
            var report = new Report
            {
                Id = Guid.NewGuid(),
                ItemId = itemId,
                UserId = userId,
                OwnerId = ownerId,
                Description = $"Declined report {i}",
                Status = ReportStatus.Declined,
                CreatedDate = DateTime.UtcNow.AddDays(-i * 3)
            };
            dbContext.Reports.Add(report);
        }
        
        await dbContext.SaveChangesAsync();
        
        var dtoValidator = new CreateReportDtoValidator(dbContext);
        var validator = new CreateReportValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Given_Request_When_DeclinedReportsAreOlderThan30Days_Then_ReturnsValid()
    {
        // Arrange
        Guid itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid ownerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
        var dto = new CreateReportDto(itemId, userId, "This item violates community guidelines");
        var request = new CreateReportRequest(dto);
        var dbContext = CreateInMemoryDbContext("create-report-" + Guid.NewGuid());
        
        // Add 6 Declined reports older than 30 days - these should NOT count
        for (int i = 0; i < 6; i++)
        {
            var report = new Report
            {
                Id = Guid.NewGuid(),
                ItemId = itemId,
                UserId = userId,
                OwnerId = ownerId,
                Description = $"Old Declined report {i}",
                Status = ReportStatus.Declined,
                CreatedDate = DateTime.UtcNow.AddDays(-31 - i)
            };
            dbContext.Reports.Add(report);
        }
        
        await dbContext.SaveChangesAsync();
        
        var dtoValidator = new CreateReportDtoValidator(dbContext);
        var validator = new CreateReportValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_Request_When_AcceptedReportExistsWithin30Days_Then_ReturnsValidationError()
    {
        // Arrange
        Guid itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid ownerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
        var dto = new CreateReportDto(itemId, userId, "This item violates community guidelines");
        var request = new CreateReportRequest(dto);
        var dbContext = CreateInMemoryDbContext("create-report-" + Guid.NewGuid());
        
        // Add the Accepted report within 30 days (should block new reports)
        var report = new Report
        {
            Id = Guid.NewGuid(),
            ItemId = itemId,
            UserId = userId,
            OwnerId = ownerId,
            Description = "Accepted report",
            Status = ReportStatus.Accepted,
            CreatedDate = DateTime.UtcNow.AddDays(-10)
        };
        dbContext.Reports.Add(report);
        
        await dbContext.SaveChangesAsync();
        
        var dtoValidator = new CreateReportDtoValidator(dbContext);
        var validator = new CreateReportValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert
        result.IsValid.Should().BeFalse();
   }

    [Fact]
    public async Task Given_Request_When_AcceptedReportExistsOlderThan30Days_Then_ReturnsValid()
    {
        // Arrange
        Guid itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid ownerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
        var dto = new CreateReportDto(itemId, userId, "This item violates community guidelines");
        var request = new CreateReportRequest(dto);
        var dbContext = CreateInMemoryDbContext("create-report-" + Guid.NewGuid());
        
        // Add the Accepted report older than 30 days (should NOT block)
        var report = new Report
        {
            Id = Guid.NewGuid(),
            ItemId = itemId,
            UserId = userId,
            OwnerId = ownerId,
            Description = "Old Accepted report",
            Status = ReportStatus.Accepted,
            CreatedDate = DateTime.UtcNow.AddDays(-31)
        };
        dbContext.Reports.Add(report);
        
        await dbContext.SaveChangesAsync();
        
        var dtoValidator = new CreateReportDtoValidator(dbContext);
        var validator = new CreateReportValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_Request_When_AllFieldsAreInvalid_Then_ReturnsMultipleValidationErrors()
    {
        // Arrange
        var dto = new CreateReportDto(Guid.Empty, Guid.Empty, "");
        var request = new CreateReportRequest(dto);
        var dbContext = CreateInMemoryDbContext("create-report-" + Guid.NewGuid());
        
        var dtoValidator = new CreateReportDtoValidator(dbContext);
        var validator = new CreateReportValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(2);
        result.Errors.Should().Contain(e => e.ErrorMessage == "Item ID is required");
        result.Errors.Should().Contain(e => e.ErrorMessage == "User ID is required");
        result.Errors.Should().Contain(e => e.ErrorMessage == "Description is required");
    }
}
