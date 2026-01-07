using AutoMapper;
using AutoMapper.QueryableExtensions;
using Backend.Features.Items.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Users.GetAllUserBookedItems;


public class GetAllUserBookedItemsHandler(ApplicationContext context, IMapper mapper, ILogger<GetAllUserBookedItemsHandler> logger) : IRequestHandler< GetAllUserBookedItemsRequest, IResult>
{
    
    public async Task<IResult> Handle(GetAllUserBookedItemsRequest request, CancellationToken cancellationToken)
    {
        var query = context.Items
            .Include(i => i.Owner)
            .Where(item => item.OwnerId == request.UserId)
            .Join(context.Bookings,
                item => item.Id,
                booking => booking.ItemId,
                (item, booking) => new { item, booking })
            .Select(x => x.item)
            .Distinct();

        var dtoList = await query
            .ProjectTo<ItemDto>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        logger.LogInformation("Retrieved {Count} booked items for user ID {UserId}.", dtoList.Count, request.UserId);
        return Results.Ok(dtoList);
    }
}