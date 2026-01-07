using MediatR;

namespace Backend.Features.Users.GetUserBookings;

public record GetUserBookingsRequest(Guid UserId): IRequest<IResult>;