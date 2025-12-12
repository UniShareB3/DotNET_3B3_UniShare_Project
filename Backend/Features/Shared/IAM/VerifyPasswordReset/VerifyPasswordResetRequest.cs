using MediatR;

namespace Backend.Features.Shared.Auth;

public record VerifyPasswordResetRequest(Guid UserId, string Code) : IRequest<IResult>;

