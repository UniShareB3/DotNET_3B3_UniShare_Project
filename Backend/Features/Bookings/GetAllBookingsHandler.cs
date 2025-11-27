using Backend.Features.Items;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Booking;

public class GetAllBookingsHandler(ApplicationContext dbContext, ILogger<GetAllBookingsHandler> logger) : IRequestHandler<GetAllBookingsRequest, IResult>
{
    public async Task<IResult> Handle(GetAllBookingsRequest request, CancellationToken cancellationToken)
    {
        var bookings = await dbContext.Bookings.ToListAsync(cancellationToken);
        
        logger.LogInformation("Retrieved {Count} bookings from the database.", bookings.Count);
        return Results.Ok(bookings);
    }
}