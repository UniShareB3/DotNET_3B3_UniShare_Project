using Backend.Features.Items.DTO;

using MediatR;

namespace Backend.Features.Items.PostItem;

public record PostItemRequest(PostItemDto Item) : IRequest<IResult>;
