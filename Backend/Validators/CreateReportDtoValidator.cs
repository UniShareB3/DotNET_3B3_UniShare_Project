using Backend.Constants;
using Backend.Features.Reports.DTO;
using FluentValidation;

namespace Backend.Validators;

public class CreateReportDtoValidator : AbstractValidator<CreateReportDto>
{
    public CreateReportDtoValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty()
            .WithMessage("Item ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(ValidationConstants.MaxDescriptionLength)
            .WithMessage($"Description cannot exceed {ValidationConstants.MaxDescriptionLength} characters");
    }
}
