namespace Backend.Features.Items.GetItem;

using MediatR;

public record GetItemRequest(Guid Id) : IRequest<IResult>;
