using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Users;

public class GetAllUserBookedItemsHandler(ApplicationContext context, ILogger<GetAllUserBookedItemsHandler> logger) : IRequestHandler< GetAllUserBookedItemsRequest, IResult>
{
    public async Task<IResult> Handle(GetAllUserBookedItemsRequest request, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User with ID {UserId} not found.", request.UserId);
            return Results.NotFound();
        }

        var bookedItems = await context.Items
            .Where(item => item.OwnerId == request.UserId)
            .Join(context.Bookings,
                item => item.Id,
                booking => booking.ItemId,
                (item, booking) => new { item, booking })
            .Select(x => x.item)
            .Distinct()
            .ToListAsync(cancellationToken);
        
        logger.LogInformation("Retrieved {Count} booked items for user ID {UserId}.", bookedItems.Count, request.UserId);
        return Results.Ok(bookedItems);
    }
}