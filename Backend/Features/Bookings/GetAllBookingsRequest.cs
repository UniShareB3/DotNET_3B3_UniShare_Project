using MediatR;

namespace Backend.Features.Booking;

public record GetAllBookingsRequest() : IRequest<IResult>;