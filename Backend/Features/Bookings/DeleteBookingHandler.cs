using Backend.Features.Booking.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Booking;

public class DeleteBookingHandler(ApplicationContext dbContext, ILogger<DeleteBookingHandler> logger): IRequestHandler<DeleteBookingRequest,IResult>
{
    public async Task<IResult> Handle(DeleteBookingRequest request, CancellationToken cancellationToken)
    {
        var booking = await dbContext.Bookings.FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);
        if (booking == null)
        {
            logger.LogError($"Booking with id {request.Id} was not found");
            return Results.NotFound();
        }

        dbContext.Bookings.Remove(booking);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation($"Booking with id {request.Id} was deleted successfully");
        return Results.Ok($"Booking with id {request.Id} was deleted successfully");
    }
}