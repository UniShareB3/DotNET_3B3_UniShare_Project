using AutoMapper;
using Backend.Data;
using Backend.Features.Users.Dtos;

namespace Backend.Mapping;

public class UserMapper : Profile
{
    public UserMapper()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => 
                src.Items != null 
                    ? src.Items.Select(i => i.Name).ToList() 
                    : new List<string>()));

        CreateMap<RegisterUserDto, User>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.UniversityId, opt => opt.Ignore());
    }
}