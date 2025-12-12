namespace Backend.Features.Items;

using MediatR;

public record GetItemRequest(Guid Id) : IRequest<IResult>;
