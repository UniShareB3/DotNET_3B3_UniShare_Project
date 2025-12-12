using MediatR;

namespace Backend.Features.Users;

public record GetUserRequest(Guid UserId) : IRequest<IResult>;
