using Backend.Data;
using Backend.Features.Reports.Enums;

namespace Backend.Features.Reports.DTO;

public record UpdateReportStatusDto(
    ReportStatus Status,
    Guid ModeratorId
);
