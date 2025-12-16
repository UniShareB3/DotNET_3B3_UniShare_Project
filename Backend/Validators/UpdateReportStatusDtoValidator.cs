using Backend.Features.Reports.DTO;
using FluentValidation;

namespace Backend.Validators;

public class UpdateReportStatusDtoValidator : AbstractValidator<UpdateReportStatusDto>
{
    private static readonly string[] ValidStatuses = { "PENDING", "ACCEPTED", "DECLINED" };

    public UpdateReportStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required")
            .Must(status => ValidStatuses.Contains(status.ToUpper()))
            .WithMessage("Status must be one of: PENDING, ACCEPTED, DECLINED");

        RuleFor(x => x.ModeratorId)
            .NotEmpty()
            .WithMessage("Moderator ID is required");
    }
}
