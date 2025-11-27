using Backend.Persistence;
using MediatR;

namespace Backend.Features.Booking;

public class GetBookingHandler(ApplicationContext dbContext) : IRequestHandler<GetBookingRequest, IResult>
{
    public Task<IResult> Handle(GetBookingRequest request, CancellationToken cancellationToken)
    {
        var booking = dbContext.Bookings.Find(request.BookingId);
        if (booking == null)
        {
            return Task.FromResult(Results.NotFound());
        }
        return Task.FromResult(Results.Ok(booking));
    }
}