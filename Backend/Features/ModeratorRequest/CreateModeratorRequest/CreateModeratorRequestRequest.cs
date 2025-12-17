using Backend.Features.ModeratorRequest.DTO;
using MediatR;

namespace Backend.Features.ModeratorRequest.CreateModeratorRequest;

public record CreateModeratorRequestRequest(CreateModeratorRequestDto Dto) : IRequest<IResult>;

