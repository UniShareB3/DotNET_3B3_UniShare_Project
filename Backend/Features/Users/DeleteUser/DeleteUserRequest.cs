using MediatR;

namespace Backend.Features.Users.DeleteUser;

public record DeleteUserRequest(Guid UserId) : IRequest<IResult>;
