using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Users;

/// <summary>
/// Handles retrieval of a booked item for a specific.
/// Queries the database to find the booking by <c>BookingId</c> and ensures the
/// item belongs to the provided user before returning the booking record.
/// </summary>
public class GetUserBookedItemHandler(ApplicationContext context) : IRequestHandler<GetUserBookedItemRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GetUserBookedItemHandler>();
    
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
            _logger.Warning("No booked item found for UserId: {UserId} and ItemId: {ItemId}", request.UserId, request.BookingId);
            return Results.NotFound();
        }

        _logger.Information("Retrieved booked item for UserId: {UserId} and ItemId: {ItemId}", request.UserId, request.BookingId);
        return Results.Ok(bookedItem);
    }
}