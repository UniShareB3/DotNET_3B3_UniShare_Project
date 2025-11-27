using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Bookings;

public class GetUserBookingsHandler(ApplicationContext dbContext, ILogger<GetUserBookingsHandler> logger) : IRequestHandler<GetUserBookingsRequest, IResult>
{
    public Task<IResult> Handle(GetUserBookingsRequest request, CancellationToken cancellationToken)
    {
        var bookings = dbContext.Bookings
            .Where(b => b.BorrowerId == request.UserId)
            .ToListAsync(cancellationToken);
        
        logger.LogInformation("Retrieved bookings for user {UserId} from the database.", request.UserId);
        return bookings.ContinueWith(task => Results.Ok(task.Result), cancellationToken);
    }
}