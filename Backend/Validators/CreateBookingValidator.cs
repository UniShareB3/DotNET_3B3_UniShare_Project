using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Persistence;
using Backend.Features.Booking;

namespace Backend.Validators;

public class CreateBookingValidator : AbstractValidator<CreateBookingRequest>
{
    private readonly ApplicationContext _context;
    private readonly ILogger<CreateBookingValidator> _logger;

    public CreateBookingValidator(ApplicationContext context, ILogger<CreateBookingValidator> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        RuleFor(x => x.Booking!.ItemId)
            .NotEmpty().WithMessage("ItemId is required.");

        RuleFor(x => x.Booking!.BorrowerId)
            .NotEmpty().WithMessage("BorrowerId is required.");

        RuleFor(x => x.Booking!.StartDate)
            .NotEmpty().WithMessage("StartDate is required.")
            .LessThan(x => x.Booking!.EndDate).WithMessage("StartDate must be before EndDate.")
            .Must(start => start > DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("StartDate cannot be in the past.");

        RuleFor(x => x.Booking!.EndDate)
            .NotEmpty().WithMessage("EndDate is required.")
            .GreaterThan(x => x.Booking!.StartDate).WithMessage("EndDate must be after StartDate.");

        RuleFor(x => x)
            .Must(x => (x.Booking!.EndDate - x.Booking!.StartDate).TotalDays <= 365)
            .WithMessage("Booking duration cannot exceed 365 days.");

        RuleFor(x => x.Booking!.RequestedOn)
            .Must(r => r <= DateTime.UtcNow.AddMinutes(10))
            .WithMessage("RequestedOn cannot be in the future.");

        RuleFor(x => x)
            .MustAsync(async (request, _) => await ItemIsAvailableForPeriod(request))
            .WithMessage("Item is not available for the requested period (overlapping booking exists).");
    }

    private async Task<bool> ItemIsAvailableForPeriod(CreateBookingRequest request)
    {
        try
        {
            var dto = request.Booking!;
            var overlapping = await _context.Bookings
                .Where(b => b.ItemId == dto.ItemId)
                .Where(b => b.Status != "Rejected" && b.Status != "Canceled")
                .Where(b => b.StartDate < dto.EndDate && dto.StartDate < b.EndDate)
                .AnyAsync();

            return !overlapping;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking item availability for booking validation.");
            return false;
        }
    }
}