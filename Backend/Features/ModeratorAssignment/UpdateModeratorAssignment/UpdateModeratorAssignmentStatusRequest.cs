using Backend.Features.ModeratorAssignment.DTO;
using MediatR;

namespace Backend.Features.ModeratorAssignment.UpdateModeratorAssignment;

public record UpdateModeratorAssignmentStatusRequest(Guid AssignmentId, UpdateModeratorAssignmentStatusDto Dto) : IRequest<IResult>;
