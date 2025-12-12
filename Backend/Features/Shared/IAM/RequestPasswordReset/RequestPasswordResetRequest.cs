using MediatR;

namespace Backend.Features.Shared.Auth;

public record RequestPasswordResetRequest(string Email) : IRequest<IResult>;

