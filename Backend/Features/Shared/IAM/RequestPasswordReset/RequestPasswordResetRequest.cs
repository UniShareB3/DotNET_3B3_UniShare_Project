using MediatR;

namespace Backend.Features.Shared.IAM.RequestPasswordReset;

public record RequestPasswordResetRequest(string Email) : IRequest<IResult>;

