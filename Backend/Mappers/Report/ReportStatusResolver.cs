using AutoMapper;
using Backend.Data;
using Backend.Features.Reports.Enums;

namespace Backend.Mappers.Report;

public class ReportStatusResolver : IValueResolver<Data.Report, object, string>
{
    public string Resolve(Data.Report source, object destination, string destMember, ResolutionContext context)
    {
        return source.Status switch
        {
            ReportStatus.PENDING => "PENDING",
            ReportStatus.ACCEPTED => "ACCEPTED",
            ReportStatus.DECLINED => "DECLINED",
            _ => "UNKNOWN"
        };
    }
}
