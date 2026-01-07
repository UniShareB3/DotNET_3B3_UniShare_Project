using Backend.Features.Reports.DTO;
using Backend.Features.Reports.Enums;
using Backend.Validators;
using FluentValidation.TestHelper;

namespace Backend.Tests.Validators;

public class UpdateReportStatusDtoValidatorTests
{
    private readonly UpdateReportStatusDtoValidator _validator = new();

    [Fact]
    public void Given_ValidDto_When_Validate_Then_NoErrors()
    {
        // Arrange
        var dto = new UpdateReportStatusDto(
            Status: ReportStatus.Accepted,
            ModeratorId: Guid.Parse("11111111-1111-1111-1111-111111111111")
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Given_ValidDtoWithPendingStatus_When_Validate_Then_NoErrors()
    {
        // Arrange
        var dto = new UpdateReportStatusDto(
            Status: ReportStatus.Pending,
            ModeratorId: Guid.Parse("22222222-2222-2222-2222-222222222222")
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Given_ValidDtoWithDeclinedStatus_When_Validate_Then_NoErrors()
    {
        // Arrange
        var dto = new UpdateReportStatusDto(
            Status: ReportStatus.Declined,
            ModeratorId: Guid.Parse("33333333-3333-3333-3333-333333333333")
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Given_InvalidStatus_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new UpdateReportStatusDto(
            Status: (ReportStatus)999,
            ModeratorId: Guid.Parse("44444444-4444-4444-4444-444444444444")
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status)
            .WithErrorMessage("Status must be a valid ReportStatus value");
    }

    [Fact]
    public void Given_EmptyModeratorId_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new UpdateReportStatusDto(
            Status: ReportStatus.Accepted,
            ModeratorId: Guid.Empty
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ModeratorId)
            .WithErrorMessage("Moderator ID is required");
    }

    [Fact]
    public void Given_EmptyModeratorIdAndInvalidStatus_When_Validate_Then_ReturnsMultipleValidationErrors()
    {
        // Arrange
        var dto = new UpdateReportStatusDto(
            Status: (ReportStatus)(-1),
            ModeratorId: Guid.Empty
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status);
        result.ShouldHaveValidationErrorFor(x => x.ModeratorId);
    }
}