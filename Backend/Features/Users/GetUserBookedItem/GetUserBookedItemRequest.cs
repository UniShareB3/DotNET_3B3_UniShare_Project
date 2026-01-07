using MediatR;

namespace Backend.Features.Users.GetUserBookedItem;

public record GetUserBookedItemRequest(Guid UserId, Guid BookingId) : IRequest<IResult> ;