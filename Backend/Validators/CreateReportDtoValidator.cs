using Backend.Features.Reports.CreateReport;
using Backend.Features.Reports.DTO;
using Backend.Features.Reports.Enums;
using Backend.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Backend.Validators;

public class CreateReportDtoValidator : AbstractValidator<CreateReportDto>
{
    private readonly ApplicationContext _applicationContext;
    public CreateReportDtoValidator(ApplicationContext applicationContext)
    {
        _applicationContext = applicationContext;
        RuleFor(x => x.ItemId)
            .NotEmpty()
            .WithMessage("Item ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters");
        
        RuleFor(x => x)
            .MustAsync(async (request, _) => await IsItemAlreadyReported(request))
            .WithMessage("A month must pass between submitting a moderator assignment again");
        
        RuleFor(x => x)
            .MustAsync(async (request, _) => await IsItemReportedAggresively(request))
            .WithMessage("You already have a moderator assignment");
        
        RuleFor(x => x)
            .MustAsync(async (request, _) => await IsItemReportAlreadyAccepted(request))
            .WithMessage("You already have an accepted report for this item in the last 30");

        RuleFor(x => x)
            .MustAsync(async (request, _) => await IsItemReportAlreadyDeclined(request))
            .WithMessage("You have exceeded the number of declined reports for this item in the last");
    }
    
    public async Task<bool> IsItemAlreadyReported(CreateReportDto request)
    {
        var existingReport = await _applicationContext.Reports
            .FirstOrDefaultAsync(
                r => r.ItemId == request.ItemId && r.UserId == request.UserId &&
                     r.Status == ReportStatus.PENDING);
        return existingReport == null;
    }
    
    public async Task<bool> IsItemReportedAggresively(CreateReportDto request)
    {
        var count = await _applicationContext.Reports
            .Where(r => r.UserId == request.UserId && r.Status == ReportStatus.DECLINED &&
                        r.ItemId == request.ItemId &&
                        r.CreatedDate >= DateTime.UtcNow.AddDays(-30))
            .CountAsync();
        return count <= 5;
    }
    
    public async Task<bool> IsItemReportAlreadyAccepted(CreateReportDto request)
    {
        var existingReport = await _applicationContext.Reports
            .FirstOrDefaultAsync(
                r => r.ItemId == request.ItemId && r.UserId == request.UserId &&
                     r.Status == ReportStatus.ACCEPTED &&
                     r.CreatedDate >= DateTime.UtcNow.AddDays(-30));
        return existingReport == null; // Return true if NO accepted report within 30 days
    }
    
    public async Task<bool> IsItemReportAlreadyDeclined(CreateReportDto request)
    {
        var existingReports = await _applicationContext.Reports
            .Where(r => r.ItemId == request.ItemId && r.UserId == request.UserId &&
                     r.Status == ReportStatus.DECLINED && r.CreatedDate >= DateTime.UtcNow.AddDays(-30))
            .CountAsync();
        return existingReports <= 5;
    }
}
