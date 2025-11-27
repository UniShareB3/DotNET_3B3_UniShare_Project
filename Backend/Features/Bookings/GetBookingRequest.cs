using MediatR;

namespace Backend.Features.Booking;

public record GetBookingRequest(Guid BookingId) : IRequest<IResult>;
