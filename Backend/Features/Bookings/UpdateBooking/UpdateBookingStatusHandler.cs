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

        booking.BookingStatus = dto.BookingStatus;

        await dbContext.SaveChangesAsync(cancellationToken);

        var bookingDto = mapper.Map<BookingDto>(booking);

        logger.LogInformation("Booking with ID {BookingId} status updated to {NewStatus}.", request.BookingId, dto.BookingStatus);
        return Results.Ok(bookingDto);
    }
}