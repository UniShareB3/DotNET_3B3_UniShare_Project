using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Backend.Features.Bookings.DTO;

namespace Backend.Features.Bookings;


//TO DO: am impresia ca aveam nevoie de jwt pentru a valida userul care face update la booking status
//       validatorul trebuie sa verifice ca bookedul la care schimb statusul apartine userului respectiva
//       validatorul trebuie sa verifice ca statusul este unul valid (enum?)
public class UpdateBookingStatusHandler(ApplicationContext dbContext, ILogger<UpdateBookingStatusHandler> logger) : IRequestHandler<UpdateBookingStatusRequest, IResult>
{ 
    public async Task<IResult> Handle(UpdateBookingStatusRequest request, CancellationToken cancellationToken)
    {
        var dto = request.BookingStatusDto;

        var booking = await dbContext.Bookings
            .Include(b => b.Item)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking == null)
        {
            logger.LogWarning("Booking with ID {BookingId} not found.", request.BookingId);
            return Results.NotFound();
        }

        booking.Status = dto.Status;
        if (dto.Status == "Approved") booking.ApprovedOn = DateTime.UtcNow;
        if (dto.Status == "Completed") booking.CompletedOn = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var itemDto = booking.Item is not null ? new ItemDto(booking.Item.Id, booking.Item.Name, booking.Item.OwnerId) : null;

        var bookingDto = new BookingDto(
            booking.Id,
            booking.ItemId,
            booking.BorrowerId,
            booking.RequestedOn,
            booking.StartDate,
            booking.EndDate,
            booking.Status,
            booking.ApprovedOn,
            booking.CompletedOn,
            itemDto
        );

        logger.LogInformation("Booking with ID {BookingId} status updated to {NewStatus}.", request.BookingId, dto.Status);
        return Results.Ok(bookingDto);
    }
}