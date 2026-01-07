using MediatR;

namespace Backend.Features.Users.GetAllUserBookedItems;

public record GetAllUserBookedItemsRequest(Guid UserId) : IRequest<IResult>;