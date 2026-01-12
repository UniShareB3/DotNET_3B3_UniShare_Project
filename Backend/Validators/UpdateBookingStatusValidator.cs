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

        // 1. Verificăm dacă user-ul are vreo legătură cu booking-ul (Owner sau Borrower)
        if (dto.UserId != booking!.BorrowerId && dto.UserId != item!.OwnerId)
        {
            context.AddFailure("User must be either the borrower or the owner of the item to update booking status");
            _logger.LogError("User is neither borrower nor owner during validation.");
            return;
        }

        // 2. Logica pentru OWNER
        if (dto.UserId == item!.OwnerId)
        {
            // Owner-ul are permisiuni depline de a schimba statusul (în limitele logicii de business, ex: nu poate aproba ceva deja completat, dar asta e validare de flux, nu de ownership)
            // Dacă a trecut de check-ul de ID, e valid din punct de vedere al permisiunilor.
            return;
        }

        // 3. Logica pentru BORROWER
        if (dto.UserId == booking.BorrowerId)
        {
            // Borrower-ul poate da cancel DOAR dacă statusul curent este Pending
            if (booking.BookingStatus != BookingStatus.Pending)
            {
                context.AddFailure("Borrower can only cancel a booking when it is still Pending.");
                _logger.LogError("Borrower can only cancel a booking when it is still Pending.");
                return;
            }

            // Borrower is allowed only to request a Canceled status when booking is Pending.
            if (dto.BookingStatus != BookingStatus.Canceled)
            {
                context.AddFailure("Borrower can only cancel a booking; they cannot change it to other statuses.");
                _logger.LogError("Borrower attempted to change booking status to a non-cancel value.");
                return;
            }

            // Dacă e Pending și e Borrower, e valid pentru Cancel.
            return;
        }

        // Nu ar trebui să ajungă aici datorită check-ului 1, dar ca siguranță:
        context.AddFailure("Unauthorized action.");
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
}