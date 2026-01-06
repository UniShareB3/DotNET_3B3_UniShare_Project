namespace Backend.Features.Items.GetUserItem;

using MediatR;

public record GetUserItemRequest(Guid UserId,Guid ItemId) : IRequest<IResult>;
