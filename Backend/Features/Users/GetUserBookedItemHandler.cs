using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Users;

/// <summary>
/// Handles retrieval of a booked item for a specific.
/// Queries the database to find the booking by <c>BookingId</c> and ensures the
/// item belongs to the provided user before returning the booking record.
/// </summary>
public class GetUserBookedItemHandler(ApplicationContext context, ILogger<GetUserBookedItemHandler> logger) : IRequestHandler<GetUserBookedItemRequest, IResult>
{
    public async Task<IResult> Handle(GetUserBookedItemRequest request, CancellationToken cancellationToken)
    {
        var bookedItem = await context.Bookings
            .Where(booking => booking.Id == request.BookingId)
            .Join( context.Items,
                   booking => booking.ItemId, 
                     item => item.Id,
                     ( booking, item) => new { booking, item } )
            .Where(joined => joined.item.OwnerId == request.UserId)
            .Select(x => x.booking)
            .FirstOrDefaultAsync(cancellationToken);
            

        if (bookedItem == null)
        {
            logger.LogWarning("No booked item found for UserId: {UserId} and ItemId: {ItemId}", request.UserId, request.BookingId);
            return Results.NotFound();
        }

        logger.LogInformation("Retrieved booked item for UserId: {UserId} and ItemId: {ItemId}", request.UserId, request.BookingId);
        return Results.Ok(bookedItem);
    }
}