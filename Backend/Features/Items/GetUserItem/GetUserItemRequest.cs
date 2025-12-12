namespace Backend.Features.Items;

using MediatR;

public record GetUserItemRequest(Guid UserId,Guid ItemId) : IRequest<IResult>;
