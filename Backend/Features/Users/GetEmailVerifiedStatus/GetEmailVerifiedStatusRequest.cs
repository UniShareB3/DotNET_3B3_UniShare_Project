namespace Backend.Features.Users;

using MediatR;

public record GetEmailVerifiedStatusRequest(Guid UserId) : IRequest<IResult>;