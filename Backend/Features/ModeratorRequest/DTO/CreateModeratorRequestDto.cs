namespace Backend.Features.ModeratorRequest.DTO;

public record CreateModeratorRequestDto(
    Guid UserId,
    string Reason
);

