using MediatR;

namespace Backend.Features.Users.AssignModeratorRole;

public record AssignModeratorRoleRequest(Guid UserId) : IRequest<IResult>;

