using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Bookings;

public class GetAllBookingsHandler(ApplicationContext dbContext) : IRequestHandler<GetAllBookingsRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GetAllBookingsHandler>();

    public async Task<IResult> Handle(GetAllBookingsRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to retrieve all bookings.");
        try
        {
            var bookings = await dbContext.Bookings.ToListAsync(cancellationToken);
            _logger.Information("Retrieved {Count} bookings.", bookings.Count);
            return Results.Ok(bookings);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "An unexpected error occurred while retrieving all bookings.");
            return Results.Problem("An unexpected error occurred while retrieving bookings.");
        }
    }
}