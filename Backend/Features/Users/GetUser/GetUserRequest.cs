using MediatR;

namespace Backend.Features.Users.GetUser;

public record GetUserRequest(Guid UserId) : IRequest<IResult>;
