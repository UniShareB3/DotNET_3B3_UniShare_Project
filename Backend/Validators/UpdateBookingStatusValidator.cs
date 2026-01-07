using Backend.Features.Bookings;
using Backend.Features.Bookings.Enums;
using FluentValidation;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Validators;

public class UpdateBookingStatusValidator : AbstractValidator<UpdateBookingStatusRequest>
{
    private readonly ApplicationContext _dbContext;
    private readonly ILogger<UpdateBookingStatusValidator> _logger;
    
    public UpdateBookingStatusValidator(ApplicationContext dbContext, ILogger<UpdateBookingStatusValidator> logger)
    {

        this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RuleFor(r => r.BookingStatusDto).NotNull().WithMessage("Request body is required.");

        RuleFor(r => r.BookingId)
            .NotEmpty().WithMessage("BookingId is required.");

        RuleFor(r => r.BookingStatusDto!.BookingStatus)
            .IsInEnum().WithMessage("BookingStatus must be a valid status (Pending, Approved, Rejected, Completed, Canceled).")
            .When(r => r.BookingStatusDto != null);

        RuleFor(r => r).Must(ValidateOwnershipAsync);
    }

    private bool ValidateOwnershipAsync(UpdateBookingStatusRequest request)
    {
        var dto = request.BookingStatusDto!;

        var booking = _dbContext.Bookings
            .Include(b => b.Item)
            .FirstOrDefault(b => b.Id == request.BookingId);

        if (booking == null)
        {
            _logger.LogError("Booking not found.");
            return false;
        }

        var item = booking.Item;
        if (item == null)
        {
            item = _dbContext.Items.Find(booking.ItemId);
            if (item == null)
            {
                _logger.LogError("Item for booking not found.");
                return false;
            }
        }

        // Allow borrower or owner to mark Completed; preserve owner-only for other statuses
        if (dto.BookingStatus == BookingStatus.Completed)
        {
            if (dto.UserId == booking.BorrowerId || dto.UserId == item.OwnerId)
            {
                return true;
            }

            _logger.LogError("Only the borrower or owner can mark a booking as Completed.");
            return false;
        }

        // Allow borrower to cancel a pending booking; owner can cancel anytime
        if (dto.BookingStatus == BookingStatus.Canceled)
        {
            // If requester is owner -> allow
            if (dto.UserId == item.OwnerId) return true;

            // If requester is borrower, only allow cancel when booking is still Pending
            if (dto.UserId == booking.BorrowerId)
            {
                if (booking.BookingStatus == BookingStatus.Pending)
                {
                    return true;
                }
                _logger.LogError("Borrower can only cancel a booking when it is still Pending.");
                return false;
            }

            _logger.LogError("Only the owner or borrower can cancel a booking (borrower only when Pending).");
            return false;
        }

        // For all other status updates, only the owner may act
        if (item.OwnerId != dto.UserId)
        {
            _logger.LogError("Only the owner of the item can change booking status.");
            return false;
        }

        return true;
    }
}