namespace Backend.Features.ModeratorAssignment.DTO;

public record ModeratorAssignmentDto(
    Guid Id,
    Guid UserId,
    string Reason,
    string Status,
    DateTime CreatedDate,
    Guid? ReviewedByAdminId,
    DateTime? ReviewedDate
);
