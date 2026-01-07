using MediatR;

namespace Backend.Features.Shared.IAM.GetRefreshTokens;

public record GetRefreshTokensRequest(Guid UserId) : IRequest<IResult>;
