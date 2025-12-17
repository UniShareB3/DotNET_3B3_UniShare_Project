namespace Backend.Features.Reports.DTO;

public record CreateReportDto(
    Guid ItemId,
    Guid UserId,
    string Description
);

