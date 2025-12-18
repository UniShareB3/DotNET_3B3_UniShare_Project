using Backend.Features.ModeratorAssignment.Enums;

namespace Backend.Features.ModeratorAssignment.DTO;

public record UpdateModeratorAssignmentStatusDto(
    ModeratorAssignmentStatus Status,
    Guid ReviewedByAdminId
);
