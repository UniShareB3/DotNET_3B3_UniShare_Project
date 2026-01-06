using AutoMapper;
using Backend.Data;
using Backend.Features.Bookings.CreateBooking;
using Backend.Features.Bookings.DTO;

namespace Backend.Mapping;

public class BookingMapper : Profile
{
    public BookingMapper()
    {
        CreateMap<CreateBookingRequest, Booking>()
            .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.Booking.ItemId))
            .ForMember(dest => dest.BorrowerId, opt => opt.MapFrom(src => src.Booking.BorrowerId))
            .ForMember(dest => dest.RequestedOn, opt => opt.MapFrom(src => src.Booking.RequestedOn))
            .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.Booking.StartDate))
            .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.Booking.EndDate));
        
        CreateMap<CreateBookingDto, Booking>()
            .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.ItemId))
            .ForMember(dest => dest.BorrowerId, opt => opt.MapFrom(src => src.BorrowerId))
            .ForMember(dest => dest.RequestedOn, opt => opt.MapFrom(src => src.RequestedOn))
            .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
            .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate));

        CreateMap<Item, ItemDto>();

        CreateMap<Booking, BookingDto>()
            .ForMember(dest => dest.Item, opt => opt.MapFrom(src => src.Item))
            .MaxDepth(1);
    }
}