using MediatR;

namespace Backend.Features.Shared.Auth;

public record AssignAdminRoleRequest(Guid UserId) : IRequest<IResult>;

