using AutoMapper;
using Backend.Data;
using Backend.Features.Reports.DTO;
using Backend.Features.Reports.Enums;

namespace Backend.Mappers.Report;

public class ReportMapper : Profile
{
    public ReportMapper()
    {
        CreateMap<Data.Report, ReportDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom<ReportStatusResolver>());
        
        CreateMap<CreateReportDto, Data.Report>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ReportStatus.PENDING))
            .ForMember(dest => dest.ModeratorId, opt => opt.Ignore())
            .ForMember(dest => dest.OwnerId, opt => opt.Ignore());
    }
}
