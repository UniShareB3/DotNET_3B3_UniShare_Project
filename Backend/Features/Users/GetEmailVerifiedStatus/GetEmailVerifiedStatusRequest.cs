namespace Backend.Features.Users.GetEmailVerifiedStatus;

using MediatR;

public record GetEmailVerifiedStatusRequest(Guid UserId) : IRequest<IResult>;