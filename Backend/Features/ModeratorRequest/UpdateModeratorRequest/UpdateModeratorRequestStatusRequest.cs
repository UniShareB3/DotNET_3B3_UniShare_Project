using Backend.Features.ModeratorRequest.DTO;
using MediatR;

namespace Backend.Features.ModeratorRequest.UpdateModeratorRequest;

public record UpdateModeratorRequestStatusRequest(Guid RequestId, UpdateModeratorRequestStatusDto Dto) : IRequest<IResult>;

