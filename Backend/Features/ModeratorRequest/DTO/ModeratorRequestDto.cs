namespace Backend.Features.ModeratorRequest.DTO;

public record ModeratorRequestDto(
    Guid Id,
    Guid UserId,
    string Reason,
    string Status,
    DateTime CreatedDate,
    Guid? ReviewedByAdminId,
    DateTime? ReviewedDate
);

