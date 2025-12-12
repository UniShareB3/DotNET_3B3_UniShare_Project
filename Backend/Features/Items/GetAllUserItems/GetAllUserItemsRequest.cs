namespace Backend.Features.Items;

using MediatR;

public record GetAllUserItemsRequest(Guid UserId) : IRequest<IResult>;
