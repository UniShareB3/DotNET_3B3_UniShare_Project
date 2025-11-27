using Backend.Persistence;
using MediatR;

namespace Backend.Features.Booking;

public class GetBookingHandler(ApplicationContext dbContext, ILogger<GetBookingHandler> logger) : IRequestHandler<GetBookingRequest, IResult>
{
    public Task<IResult> Handle(GetBookingRequest request, CancellationToken cancellationToken)
    {
        var booking = dbContext.Bookings.Find(request.BookingId);
        if (booking == null)
        {
            logger.LogError("Booking with ID {BookingId} not found.", request.BookingId);
            return Task.FromResult(Results.NotFound());
        }
        
        logger.LogInformation("Booking with ID {BookingId} was found.", request.BookingId);
        return Task.FromResult(Results.Ok(booking));
    }
}