using Backend.Features.ModeratorAssignment.DTO;
using Backend.Features.ModeratorAssignment.Enums;
using FluentValidation;

namespace Backend.Validators;

public class UpdateModeratorAssignmentStatusDtoValidator : AbstractValidator<UpdateModeratorAssignmentStatusDto>
{
    public UpdateModeratorAssignmentStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Status must be a valid ModeratorAssignmentStatus value");

        RuleFor(x => x.ReviewedByAdminId)
            .NotEmpty()
            .WithMessage("Reviewer Admin ID is required");
    }
}
