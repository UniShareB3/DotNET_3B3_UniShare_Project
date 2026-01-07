using MediatR;

namespace Backend.Features.Shared.IAM.RefreshToken;

public record RefreshTokenRequest(string RefreshToken) : IRequest<IResult>;
