using Backend.Data;
using Backend.Features.Reports.Enums;

namespace Backend.Features.Reports.DTO;

public record UpdateReportStatusDto(
    string Status,
    Guid ModeratorId
);
