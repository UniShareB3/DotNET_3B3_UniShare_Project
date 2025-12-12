using MediatR;

namespace Backend.Features.Bookings;

public record GetBookingRequest(Guid BookingId) : IRequest<IResult>;
