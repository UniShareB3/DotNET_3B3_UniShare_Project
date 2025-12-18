using Backend.Features.Reports.CreateReport;
using Backend.Features.Reports.Enums;
using Backend.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Backend.Validators;

public class CreateReportValidator : AbstractValidator<CreateReportRequest>
{
    public CreateReportValidator(CreateReportDtoValidator reportDtoValidator)
    {
        RuleFor(x => x.Dto)
            .SetValidator(reportDtoValidator);
        
    }
    
   
}