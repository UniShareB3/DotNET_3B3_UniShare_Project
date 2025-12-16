using Backend.Features.ModeratorRequest.DTO;
using FluentValidation;

namespace Backend.Validators;

public class CreateModeratorRequestDtoValidator : AbstractValidator<CreateModeratorRequestDto>
{
    public CreateModeratorRequestDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required")
            .MaximumLength(1000)
            .WithMessage("Reason cannot exceed 1000 characters");
    }
}

