using Backend.Constants;
using Backend.Features.Reports.CreateReport;
using Backend.Features.Reports.DTO;
using Backend.Features.Reports.Enums;
using Backend.Persistence;
using FluentValidation;


namespace Backend.Validators;

public class CreateReportRequestDtoValidator : AbstractValidator<CreateReportDto>
{
    private ApplicationContext dbContext;
    
    public CreateReportRequestDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("Reported Entity ID is required");

        RuleFor(x => x.ItemId)
            .NotEmpty()
            .WithMessage("Reporting User ID is required");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(1000)
            .WithMessage("Reason cannot exceed 1000 characters");

        RuleFor(dto => dto).Must(IsReportAcceptedPresent)
            .WithMessage("An accepted report for this item by the same user already exists.");
        
        RuleFor(dto => dto).Must(IsReportDeclinedPresentMultipleTimes)
            .WithMessage("An accepted report for this item by the same user already exists.");
    }
    
    private bool IsReportAcceptedPresent(CreateReportDto dto)
    {
         var isPresent = dbContext.Reports
             .Any(r => r.ItemId == dto.ItemId && r.UserId == dto.UserId 
                                              && r.Status == ReportStatus.ACCEPTED);
         return !isPresent;
    }

    private bool IsReportDeclinedPresentMultipleTimes(CreateReportDto dto)
    {
        var reports = dbContext.Reports
            .Where(r => r.ItemId == dto.ItemId && r.UserId == dto.UserId 
                                               && r.Status == ReportStatus.DECLINED)
            .OrderBy(r => r.CreatedDate)
            .Take(ValidationConstants.MaxDeclinedReportsPerItem)
            .ToList();
        
        var time = reports.Last().CreatedDate - reports.First().CreatedDate;
        if (time.TotalDays <= ValidationConstants.MaxDaysForPendingReports)
            return false; 

        return true;
    }
    
}