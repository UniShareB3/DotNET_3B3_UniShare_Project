using Backend.Features.Items.DTO;
using MediatR;

namespace Backend.Features.Items.PatchItem;

public record PatchItemRequest(Guid ItemId, PatchItemDto PatchItemDto) : IRequest<IResult>;

