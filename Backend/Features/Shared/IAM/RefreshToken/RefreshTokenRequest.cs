using MediatR;

namespace Backend.Features.Shared.Auth;

public record RefreshTokenRequest(string RefreshToken) : IRequest<IResult>;
