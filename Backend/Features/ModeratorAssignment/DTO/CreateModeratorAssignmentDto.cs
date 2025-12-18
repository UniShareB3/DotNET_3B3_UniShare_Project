namespace Backend.Features.ModeratorAssignment.DTO;

public record CreateModeratorAssignmentDto(
    Guid UserId,
    string Reason
);
