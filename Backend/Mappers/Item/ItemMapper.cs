        using AutoMapper;
        using Backend.Data;
        using Backend.Features.Items.DTO;
        using Backend.Features.Items.Enums;

        namespace Backend.Mapping;

        public class ItemMapper:Profile
        {
            public ItemMapper()
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
                        dest => dest.OwnerName,
                        opt => opt.MapFrom(src => (src.Owner.FirstName + " " + src.Owner.LastName).Trim())
                    )
                    .MaxDepth(1);
            }
        }