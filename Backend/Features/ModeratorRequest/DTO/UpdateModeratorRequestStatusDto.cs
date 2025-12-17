using Backend.Features.ModeratorRequest.Enums;

namespace Backend.Features.ModeratorRequest.DTO;

public record UpdateModeratorRequestStatusDto(
    ModeratorRequestStatus Status,
    Guid ReviewedByAdminId
);

