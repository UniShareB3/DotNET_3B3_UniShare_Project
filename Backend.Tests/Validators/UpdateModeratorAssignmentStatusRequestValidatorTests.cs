using Backend.Features.ModeratorAssignment.DTO;
using Backend.Features.ModeratorAssignment.Enums;
using Backend.Features.ModeratorAssignment.UpdateModeratorAssignment;
using Backend.Validators;
using FluentValidation.TestHelper;

namespace Backend.Tests.Validators;

public class UpdateModeratorAssignmentStatusRequestValidatorTests
{
    private readonly UpdateModeratorAssignmentStatusRequestValidator _requestValidator;

    public UpdateModeratorAssignmentStatusRequestValidatorTests()
    {
        var dtoValidator = new UpdateModeratorAssignmentStatusDtoValidator();
        _requestValidator = new UpdateModeratorAssignmentStatusRequestValidator(dtoValidator);
    }

    [Fact]
    public void Given_ValidRequest_When_Validating_Then_NoErrors()
    {
        // Arrange
        var assignmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var reviewedByAdminId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        
        var dto = new UpdateModeratorAssignmentStatusDto(
            Status: ModeratorAssignmentStatus.Accepted,
            ReviewedByAdminId: reviewedByAdminId
        );
        
        var request = new UpdateModeratorAssignmentStatusRequest(assignmentId, dto);

        // Act
        var result = _requestValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Given_InvalidDto_When_Validating_Then_ReturnsValidationErrors()
    {
        // Arrange
        var assignmentId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        
        // Create a DTO with invalid status and empty ReviewedByAdminId
        var dto = new UpdateModeratorAssignmentStatusDto(
            Status: (ModeratorAssignmentStatus)999,
            ReviewedByAdminId: Guid.Empty
        );
        
        var request = new UpdateModeratorAssignmentStatusRequest(assignmentId, dto);

        // Act
        var result = _requestValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Dto.Status)
            .WithErrorMessage("Status must be a valid ModeratorAssignmentStatus value");
        result.ShouldHaveValidationErrorFor(x => x.Dto.ReviewedByAdminId)
            .WithErrorMessage("Reviewer Admin ID is required");
    }

    [Fact]
    public void Given_DtoWithEmptyReviewedByAdminId_When_Validating_Then_ReturnsValidationError()
    {
        // Arrange
        var assignmentId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        
        var dto = new UpdateModeratorAssignmentStatusDto(
            Status: ModeratorAssignmentStatus.Rejected,
            ReviewedByAdminId: Guid.Empty
        );
        
        var request = new UpdateModeratorAssignmentStatusRequest(assignmentId, dto);

        // Act
        var result = _requestValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Dto.ReviewedByAdminId)
            .WithErrorMessage("Reviewer Admin ID is required");
    }
}

