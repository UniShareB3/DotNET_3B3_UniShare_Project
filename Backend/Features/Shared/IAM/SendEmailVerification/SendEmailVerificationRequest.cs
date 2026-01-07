using MediatR;

namespace Backend.Features.Shared.IAM.SendEmailVerification;

public record SendEmailVerificationRequest(Guid UserId) : IRequest<IResult>;
