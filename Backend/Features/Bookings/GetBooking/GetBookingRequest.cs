using MediatR;

namespace Backend.Features.Bookings.GetBooking;

public record GetBookingRequest(Guid BookingId) : IRequest<IResult>;
