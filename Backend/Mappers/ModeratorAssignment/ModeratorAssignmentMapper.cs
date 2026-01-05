using AutoMapper;
using Backend.Features.ModeratorAssignment.DTO;
using Backend.Features.ModeratorAssignment.Enums;

namespace Backend.Mappers.ModeratorAssignment;

public class ModeratorAssignmentMapper : Profile
{
    public ModeratorAssignmentMapper()
    {
        CreateMap<Data.ModeratorAssignment, ModeratorAssignmentDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom<ModeratorAssignmentStatusResolver>());
        
        CreateMap<CreateModeratorAssignmentDto, Data.ModeratorAssignment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ModeratorAssignmentStatus.PENDING))
            .ForMember(dest => dest.ReviewedByAdminId, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewedDate, opt => opt.Ignore());
    }
}
