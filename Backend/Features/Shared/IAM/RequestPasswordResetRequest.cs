using MediatR;

namespace Backend.Features.Shared.Auth;

public record RequestPasswordResetRequest(Guid UserId) : IRequest<IResult>;

