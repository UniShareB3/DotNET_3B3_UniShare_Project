using AutoMapper;
using Backend.Data;
using Backend.Features.Items.DTO;
using Backend.Features.Items.Enums;
using Backend.Services.AzureStorage;

namespace Backend.Mapping;

public class ItemMapper : Profile
{
    public ItemMapper() {}
    public ItemMapper(IAzureStorageService azureStorageService)
    {
        CreateMap<PostItemDto, Item>()
            .ForMember(
                dest => dest.Category,
                opt => opt.MapFrom(src => Enum.Parse<ItemCategory>(src.Category, true))
            )
            .ForMember(
                dest => dest.Condition,
                opt => opt.MapFrom(src => Enum.Parse<ItemCondition>(src.Condition, true))
            )
            .ForMember(
                dest => dest.Description,
                opt => opt.MapFrom(src => src.Description)
                )
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsAvailable, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.Bookings, opt => opt.Ignore());

        CreateMap<Item, ItemDto>()
            .ForMember(
                dest => dest.Category,
                opt => opt.MapFrom(src => src.Category.ToString())
            )
            .ForMember(
                dest => dest.Condition,
                opt => opt.MapFrom(src => src.Condition.ToString())
            )
            .ForMember(
                dest => dest.OwnerId,
                opt => opt.MapFrom(src => src.OwnerId)
            )
            .ForMember(
                dest => dest.OwnerName, 
                opt => opt.MapFrom(src => src.Owner != null 
                    ? (src.Owner.FirstName + " " + src.Owner.LastName).Trim() 
                    : "Unknown Owner") 
            )
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.BlobName) 
                    ? azureStorageService.GenerateReadSasUrl(src.BlobName, TimeSpan.FromHours(1))
                    : src.ImageUrl))
            .MaxDepth(1);
    }
}