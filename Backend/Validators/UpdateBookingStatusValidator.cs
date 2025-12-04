using Backend.Features.Bookings;
using FluentValidation;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Validators;

public class UpdateBookingStatusValidator : AbstractValidator<UpdateBookingStatusRequest>
{
    private readonly ApplicationContext dbContext;
    private readonly ILogger<UpdateBookingStatusValidator> logger;
    
    public UpdateBookingStatusValidator(ApplicationContext dbContext, ILogger<UpdateBookingStatusValidator> logger)
    {

        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RuleFor(r => r.BookingStatusDto).NotNull().WithMessage("Request body is required.");

        RuleFor(r => r.BookingId)
            .NotEmpty().WithMessage("BookingId is required.");

        RuleFor(r => r).Must(ValidateOwnershipAsync);
    }

    private bool ValidateOwnershipAsync(UpdateBookingStatusRequest request)
    {
        var dto = request.BookingStatusDto!; // validated by RuleFor

        var booking = dbContext.Bookings
            .Include(b => b.Item)
            .FirstOrDefault(b => b.Id == request.BookingId);

        if (booking == null)
        {
            logger.LogError("Booking not found.");
            return false;
        }

        var item = booking.Item;
        if (item == null)
        {
            item = dbContext.Items.Find(booking.ItemId);
            if (item == null)
            {
                logger.LogError("Item for booking not found.");
                return false;
            }
        }

        if (item.OwnerId != dto.UserId)
        {
            logger.LogError("Only the owner of the item can change booking status.");
            return false;
        }

        return true;
    }
}