using MediatR;

namespace Backend.Features.Booking.DTO;

public record DeleteBookingRequest(Guid Id): IRequest<IResult>;