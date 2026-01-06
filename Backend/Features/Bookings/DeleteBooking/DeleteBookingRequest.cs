using MediatR;

namespace Backend.Features.Bookings.DeleteBooking;

public record DeleteBookingRequest(Guid Id): IRequest<IResult>;