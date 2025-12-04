using AutoMapper;
using Backend.Data;
using Backend.Features.Universities.DTO;

namespace Backend.Mapping;

public class UniversityMapper : Profile
{
    public UniversityMapper()
    {
        CreateMap<University, UniversityDto>();
        
        CreateMap<UniversityDto, University>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

        CreateMap<PostUniversityDto, University>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
    }
}