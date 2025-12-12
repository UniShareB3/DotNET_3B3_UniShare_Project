using MediatR;

namespace Backend.Features.Shared.Auth;

public record GetRefreshTokensRequest(Guid UserId) : IRequest<IResult>;
