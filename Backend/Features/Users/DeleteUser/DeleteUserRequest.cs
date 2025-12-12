using MediatR;

namespace Backend.Features.Users;

public record DeleteUserRequest(Guid UserId) : IRequest<IResult>;
