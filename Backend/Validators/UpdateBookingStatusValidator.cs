using Backend.Data;
using Backend.Features.Bookings.DTO;
using Backend.Features.Bookings.Enums;
using Backend.Features.Bookings.UpdateBooking;
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
        _dbContext = dbContext;
        _logger = logger;

        RuleFor(r => r.BookingStatusDto).NotNull().WithMessage("Request body is required.").ChildRules(dto => {
           dto.RuleFor(d => d.BookingStatus)
                .IsInEnum()
                .WithMessage("BookingStatus must be a valid status...");
        });

        RuleFor(r => r.BookingId)
            .NotEmpty().WithMessage("BookingId is required.");
        
        RuleFor(r => r).CustomAsync(ValidateOwnershipAsync);
    }

    private async Task ValidateOwnershipAsync(UpdateBookingStatusRequest request,
        ValidationContext<UpdateBookingStatusRequest> context, CancellationToken cancellationToken)
    {
        var dto = request.BookingStatusDto;

        var booking = await GetBookingWithItemAsync(request.BookingId, cancellationToken);
        if (!ValidateBookingExists(booking, context)) return;

        var item = await GetItemByBookingAsync(booking!, cancellationToken);
        if (!ValidateItemExists(item, context)) return;

        if (!ValidateOwnerIsDifferentFromBorrower(booking!, item!, dto, context)) return;

        if (!ValidateOwnerAndBorrowerCancelStatus(booking!, item!, dto,context)) return;

        ValidateOwnerOnly(item!, dto, context);
    }

    private async Task<Booking?> GetBookingWithItemAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        return await _dbContext.Bookings
            .Include(b => b.Item)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken: cancellationToken);
    }

    private async Task<Item?> GetItemByBookingAsync(Booking booking, CancellationToken cancellationToken)
    {
        return await _dbContext.Items
            .FirstOrDefaultAsync(i => i.Id == booking.ItemId, cancellationToken: cancellationToken);
    }

    private bool ValidateBookingExists(Booking? booking, ValidationContext<UpdateBookingStatusRequest> context)
    {
        if (booking != null) return true;
        context.AddFailure("Booking does not exist");
        _logger.LogError( "Booking not found during validation.");
        return false;
    }

    private bool ValidateItemExists(Item? item, ValidationContext<UpdateBookingStatusRequest> context)
    {
        if (item != null) return true;
        context.AddFailure("Item associated with booking does not exist");
        _logger.LogError( "Item not found during validation.");
        return false;
    }

    private bool ValidateOwnerIsDifferentFromBorrower(
        Booking booking,
        Item item,
        UpdateBookingStatusDto dto,
        ValidationContext<UpdateBookingStatusRequest> context)
    {
        if (dto.UserId == booking.BorrowerId || dto.UserId == item.OwnerId) return true;
        context.AddFailure("User must be either the borrower or the owner of the item to update booking status");
        _logger.LogError(" User is neither borrower nor owner during validation.");
        return false;
    }

    private bool ValidateOwnerAndBorrowerCancelStatus(
        Booking booking,
        Item item,
        UpdateBookingStatusDto dto,
        ValidationContext<UpdateBookingStatusRequest> context)
    {
        if (dto.UserId == item.OwnerId) return true;
        
        if (dto.UserId == booking.BorrowerId)
        {
            if (booking.BookingStatus == BookingStatus.Pending)
            {
                return true;
            }

            context.AddFailure("Borrower can only cancel a booking when it is still Pending.");
            _logger.LogError("Borrower can only cancel a booking when it is still Pending.");
            return false;
        }

        context.AddFailure("Only the owner or borrower can cancel a booking (borrower only when Pending).");
        _logger.LogError(" Only the owner or borrower during validation.");
        return false;
    }
    
    private void ValidateOwnerOnly(
        Item item,
        UpdateBookingStatusDto dto,
        ValidationContext<UpdateBookingStatusRequest> context)
    {
        if (item.OwnerId == dto.UserId) return;
        context.AddFailure("Only the owner of the item can change booking status.");
        _logger.LogError(" Only the owner of the item can change booking status."); 
    }

}