using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Bookings;

public class GetUserBookingsHandler(ApplicationContext dbContext) : IRequestHandler<GetUserBookingsRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GetUserBookingsHandler>();
    
    public Task<IResult> Handle(GetUserBookingsRequest request, CancellationToken cancellationToken)
    {
        var bookings = dbContext.Bookings
            .Where(b => b.BorrowerId == request.UserId)
            .ToListAsync(cancellationToken);
        
        _logger.Information("Retrieved bookings for user {UserId} from the database.", request.UserId);
        return bookings.ContinueWith(task => Results.Ok(task.Result), cancellationToken);
    }
}