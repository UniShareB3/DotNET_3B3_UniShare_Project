using AutoMapper;
using Backend.Features.Reports.Enums;

namespace Backend.Mappers.Report;

public abstract class ReportStatusResolver : IValueResolver<Data.Report, object, string>
{
    public string Resolve(Data.Report source, object destination, string destMember, ResolutionContext context)
    {
        return source.Status switch
        {
            ReportStatus.Pending => "PENDING",
            ReportStatus.Accepted => "ACCEPTED",
            ReportStatus.Declined => "DECLINED",
            _ => "UNKNOWN"
        };
    }
}
