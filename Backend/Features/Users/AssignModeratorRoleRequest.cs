using MediatR;

namespace Backend.Features.Users;

public record AssignModeratorRoleRequest(Guid UserId) : IRequest<IResult>;

