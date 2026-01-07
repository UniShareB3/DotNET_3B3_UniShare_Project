namespace Backend.Features.Items.GetAllUserItems;

using MediatR;

public record GetAllUserItemsRequest(Guid UserId) : IRequest<IResult>;
