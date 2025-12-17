using AutoMapper;
using Backend.Features.ModeratorRequest.DTO;
using Backend.Features.ModeratorRequest.Enums;

namespace Backend.Mappers.ModeratorRequest;

public class ModeratorRequestMapper : Profile
{
    public ModeratorRequestMapper()
    {
        CreateMap<Data.ModeratorRequest, ModeratorRequestDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom<ModeratorRequestStatusResolver>());
        
        CreateMap<CreateModeratorRequestDto, Data.ModeratorRequest>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ModeratorRequestStatus.PENDING))
            .ForMember(dest => dest.ReviewedByAdminId, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewedDate, opt => opt.Ignore());
    }
}

