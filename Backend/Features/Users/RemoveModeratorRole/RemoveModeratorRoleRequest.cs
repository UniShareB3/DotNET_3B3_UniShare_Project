using MediatR;

namespace Backend.Features.Users.RemoveModeratorRole;

public record RemoveModeratorRoleRequest(Guid UserId) : IRequest<IResult>;

