namespace Backend.Features.Items;

using MediatR;

public record GetAllItemsRequest() : IRequest<IResult>;
