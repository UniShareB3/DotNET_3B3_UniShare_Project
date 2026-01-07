using MediatR;

namespace Backend.Features.Shared.IAM.VerifyPasswordReset;

public record VerifyPasswordResetRequest(Guid UserId, string Code) : IRequest<IResult>;

