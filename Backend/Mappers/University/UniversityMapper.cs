using AutoMapper;
using Backend.Data;
using Backend.Features.Universities.DTO;

namespace Backend.Mappers.University;

public class UniversityMapper : Profile
{
    public UniversityMapper()
    {
        CreateMap<Data.University, UniversityDto>();
        
        CreateMap<UniversityDto, Data.University>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

        CreateMap<PostUniversityDto, Data.University>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
    }
}