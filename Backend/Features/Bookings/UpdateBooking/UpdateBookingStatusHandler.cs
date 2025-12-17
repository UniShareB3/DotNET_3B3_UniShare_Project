using AutoMapper;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Backend.Features.Bookings.DTO;
using Backend.Features.Bookings.Enums;

namespace Backend.Features.Bookings;


public class UpdateBookingStatusHandler(ApplicationContext dbContext, ILogger<UpdateBookingStatusHandler> logger, IMapper mapper) : IRequestHandler<UpdateBookingStatusRequest, IResult>
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

        // Special-case: if borrower cancels a pending booking, remove the booking record
        if (dto.BookingStatus == BookingStatus.Canceled && dto.UserId == booking.BorrowerId && booking.BookingStatus == BookingStatus.Pending)
        {
            dbContext.Bookings.Remove(booking);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Booking with ID {BookingId} removed by borrower.", request.BookingId);
            return Results.NoContent();
        }

        booking.BookingStatus = dto.BookingStatus;

        // If the new status is Completed, set CompletedOn (and optionally record who completed it if provided)
        if (dto.BookingStatus == BookingStatus.Completed)
        {
            if (booking.CompletedOn == null)
            {
                booking.CompletedOn = DateTime.UtcNow;
            }
            // If DTO contains UserId, optionally set an audit field if Booking has one (CompletedBy)
            try {
                var completedByProp = booking.GetType().GetProperty("CompletedBy");
                if (completedByProp != null && dto.UserId != Guid.Empty) {
                    completedByProp.SetValue(booking, dto.UserId);
                }
            } catch { /* ignore if property doesn't exist */ }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var bookingDto = mapper.Map<BookingDto>(booking);

        logger.LogInformation("Booking with ID {BookingId} status updated to {NewStatus}.", request.BookingId, dto.BookingStatus);
        return Results.Ok(bookingDto);
    }
}