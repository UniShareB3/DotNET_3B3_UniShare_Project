using MediatR;

namespace Backend.Features.ModeratorRequest.GetAllModeratorRequests;

public record GetAllModeratorRequestsRequest : IRequest<IResult>;

