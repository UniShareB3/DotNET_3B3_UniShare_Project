using Backend.Features.Reports.Enums;

namespace Backend.Mappers.Report;

/// <summary>
/// Resolver to convert string status to ReportStatus enum
/// </summary>
public static class ReportStatusStringResolver
{
    public static ReportStatus ResolveStatus(string status)
    {
        return status.ToUpper() switch
        {
            "PENDING" => ReportStatus.PENDING,
            "ACCEPTED" => ReportStatus.ACCEPTED,
            "DECLINED" => ReportStatus.DECLINED,
            _ => throw new ArgumentException($"Invalid report status: {status}", nameof(status))
        };
    }
}

