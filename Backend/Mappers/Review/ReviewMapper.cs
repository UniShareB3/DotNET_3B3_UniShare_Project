using AutoMapper;
using Backend.Features.Review.CreateReview;
using Backend.Features.Review.DTO;

namespace Backend.Mappers.Review;

public class ReviewMapper : Profile
{
    public ReviewMapper()
    {
        CreateMap<CreateReviewRequest, Data.Review>()
            .ForMember(dest => dest.BookingId, opt => opt.MapFrom(src => src.Review.BookingId))
            .ForMember(dest => dest.ReviewerId, opt => opt.MapFrom(src => src.Review.ReviewerId))
            .ForMember(dest => dest.TargetUserId, opt => opt.MapFrom(src => src.Review.TargetUserId))
            .ForMember(dest => dest.TargetItemId, opt => opt.MapFrom(src => src.Review.TargetItemId))
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Review.Rating))
            .ForMember(dest => dest.Comment, opt => opt.MapFrom(src => src.Review.Comment))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.Review.CreatedAt));
        
        CreateMap<CreateReviewDto, Data.Review>()
            .ForMember(dest => dest.BookingId, opt => opt.MapFrom(src => src.BookingId))
            .ForMember(dest => dest.ReviewerId, opt => opt.MapFrom(src => src.ReviewerId))
            .ForMember(dest => dest.TargetUserId, opt => opt.MapFrom(src => src.TargetUserId))
            .ForMember(dest => dest.TargetItemId, opt => opt.MapFrom(src => src.TargetItemId))
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
            .ForMember(dest => dest.Comment, opt => opt.MapFrom(src => src.Comment))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));
        
        CreateMap<Data.Review, ReviewDto>();
    }
}