using Backend.Features.Reports.CreateReport;
using FluentValidation;

namespace Backend.Validators;

public class CreateReportValidator : AbstractValidator<CreateReportRequest>
{
    public CreateReportValidator(CreateReportDtoValidator reportDtoValidator)
    {
        RuleFor(x => x.Dto)
            .SetValidator(reportDtoValidator);
        
    }
    
   
}