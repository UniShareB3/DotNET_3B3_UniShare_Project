using MediatR;

namespace Backend.Features.Shared.Auth;

public record SendEmailVerificationRequest(Guid UserId) : IRequest<IResult>;
