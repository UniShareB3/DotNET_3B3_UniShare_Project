using MediatR;

namespace Backend.Features.Bookings;

public record GetAllBookingsRequest() : IRequest<IResult>;