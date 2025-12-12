namespace Backend.Features.Items;

using MediatR;

public record DeleteItemRequest(Guid Id) : IRequest<IResult>;
