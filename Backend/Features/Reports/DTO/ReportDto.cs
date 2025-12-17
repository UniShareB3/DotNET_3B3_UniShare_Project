using Backend.Data;

namespace Backend.Features.Reports.DTO;

public record ReportDto(
    Guid Id,
    Guid ItemId,
    Guid OwnerId,
    Guid UserId,
    string Description,
    DateTime CreatedDate,
    string Status,
    Guid? ModeratorId
);
