using MediatR;

namespace Backend.Features.Shared.IAM.AssignAdminRole;

public record AssignAdminRoleRequest(Guid UserId) : IRequest<IResult>;

