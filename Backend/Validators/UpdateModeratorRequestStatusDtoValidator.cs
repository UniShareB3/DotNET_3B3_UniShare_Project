using Backend.Features.ModeratorRequest.DTO;
using Backend.Features.ModeratorRequest.Enums;
using FluentValidation;

namespace Backend.Validators;

public class UpdateModeratorRequestStatusDtoValidator : AbstractValidator<UpdateModeratorRequestStatusDto>
{
    public UpdateModeratorRequestStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Status must be a valid ModeratorRequestStatus value");

        RuleFor(x => x.ReviewedByAdminId)
            .NotEmpty()
            .WithMessage("Reviewer Admin ID is required");
    }
}

