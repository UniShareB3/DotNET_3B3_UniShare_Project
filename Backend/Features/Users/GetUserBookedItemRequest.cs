using MediatR;

namespace Backend.Features.Users;

public record GetUserBookedItemRequest(Guid UserId, Guid BookingId) : IRequest<IResult> ;