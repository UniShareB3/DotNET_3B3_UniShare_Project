using MediatR;

namespace Backend.Features.Bookings;

public record GetUserBookingsRequest(Guid UserId): IRequest<IResult>;