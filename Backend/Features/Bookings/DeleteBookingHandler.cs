using Backend.Features.Booking.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Booking;

public class DeleteBookingHandler(ApplicationContext context): IRequestHandler<DeleteBookingRequest,IResult>
{
    public async Task<IResult> Handle(DeleteBookingRequest request, CancellationToken cancellationToken)
    {
        var booking = await context.Bookings.FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);
        if (booking == null)
        {
            throw new KeyNotFoundException("Booking not found");
        }

        context.Bookings.Remove(booking);
        await context.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}