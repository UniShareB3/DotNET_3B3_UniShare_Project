using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Bookings;

public class DeleteBookingHandler(ApplicationContext dbContext): IRequestHandler<DeleteBookingRequest,IResult>
{
    private readonly ILogger _logger=Log.ForContext<DeleteBookingHandler>();
    public async Task<IResult> Handle(DeleteBookingRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to delete booking with id {RequestId}", request.Id);
        try
        {
            var booking = await dbContext.Bookings.FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);
            if (booking == null)
            {
                _logger.Error("Booking with id {RequestId} was not found", request.Id);
                return Results.NotFound();
            }

            dbContext.Bookings.Remove(booking);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.Information("Booking with id {RequestId} was deleted successfully", request.Id);
            return Results.Ok($"Booking with id {request.Id} was deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "An error occurred while deleting booking with id {RequestId}", request.Id);
            return Results.Problem("An unexpected error occurred while deleting the booking.");
        }
    }
}