namespace Backend.Features.Items.DeleteItem;

using MediatR;

public record DeleteItemRequest(Guid Id) : IRequest<IResult>;
