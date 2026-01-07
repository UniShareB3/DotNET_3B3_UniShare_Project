using Backend.Features.Reports.DTO;
using FluentValidation;

namespace Backend.Validators;

public class UpdateReportStatusDtoValidator : AbstractValidator<UpdateReportStatusDto>
{
    public UpdateReportStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Status must be a valid ReportStatus value");

        RuleFor(x => x.ModeratorId)
            .NotEmpty()
            .WithMessage("Moderator ID is required");
    }
}
