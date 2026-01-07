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
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
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
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        
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
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        
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
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
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
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        
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
        var dbContext = CreateInMemoryDbContext("14444444-4444-4444-4444-444444444444");
        
        // Add an existing pending report
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
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        // Add 3 declined reports within the last 30 days
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
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        // Add 6 declined reports within the last 30 days
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
        result.Errors.Should().Contain(e => e.ErrorMessage == "You already have a moderator assignment");
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
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        // Add 6 declined reports older than 30 days
        for (int i = 0; i < 6; i++)
        {
            var report = new Report
            {
                Id = Guid.NewGuid(),
                ItemId = itemId,
                UserId = userId,
                OwnerId = ownerId,
                Description = $"Old declined report {i}",
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
    public async Task Given_Request_When_AcceptedReportsExist_Then_ReturnsValid()
    {
        // Arrange
        Guid itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid ownerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
        var dto = new CreateReportDto(itemId, userId, "This item violates community guidelines");
        var request = new CreateReportRequest(dto);
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        // Add accepted reports (should not block new reports)
        for (int i = 0; i < 3; i++)
        {
            var report = new Report
            {
                Id = Guid.NewGuid(),
                ItemId = itemId,
                UserId = userId,
                OwnerId = ownerId,
                Description = $"Accepted report {i}",
                Status = ReportStatus.Accepted,
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
    public async Task Given_Request_When_AllFieldsAreInvalid_Then_ReturnsMultipleValidationErrors()
    {
        // Arrange
        var dto = new CreateReportDto(Guid.Empty, Guid.Empty, "");
        var request = new CreateReportRequest(dto);
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        var dtoValidator = new CreateReportDtoValidator(dbContext);
        var validator = new CreateReportValidator(dtoValidator);
        
        // Act
        var result = await validator.ValidateAsync(request);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain(e => e.ErrorMessage == "Item ID is required");
        result.Errors.Should().Contain(e => e.ErrorMessage == "User ID is required");
        result.Errors.Should().Contain(e => e.ErrorMessage == "Description is required");
    }
}

