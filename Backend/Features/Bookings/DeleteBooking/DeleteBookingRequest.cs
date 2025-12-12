using MediatR;

namespace Backend.Features.Bookings;

public record DeleteBookingRequest(Guid Id): IRequest<IResult>;