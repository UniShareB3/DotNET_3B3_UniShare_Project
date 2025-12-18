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
}

