using Backend.Features.Reports.CreateReport;
using FluentValidation;

namespace Backend.Validators;

public class CreateReportRequestValidator : AbstractValidator<CreateReportRequest>
{
    public CreateReportRequestValidator(CreateReportRequestDtoValidator validator)
    {
        RuleFor(x => x.Dto)
            .SetValidator(validator);
    }
}