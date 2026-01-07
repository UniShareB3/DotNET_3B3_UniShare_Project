using AutoMapper;
using Backend.Features.Bookings.CreateBooking;
using Backend.Features.Bookings.DTO;

namespace Backend.Mappers.Booking;

public class BookingMapper : Profile
{
    public BookingMapper()
    {
        CreateMap<CreateBookingRequest, Data.Booking>()
            .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.Booking.ItemId))
            .ForMember(dest => dest.BorrowerId, opt => opt.MapFrom(src => src.Booking.BorrowerId))
            .ForMember(dest => dest.RequestedOn, opt => opt.MapFrom(src => src.Booking.RequestedOn))
            .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.Booking.StartDate))
            .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.Booking.EndDate));
        
        CreateMap<CreateBookingDto, Data.Booking>()
            .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.ItemId))
            .ForMember(dest => dest.BorrowerId, opt => opt.MapFrom(src => src.BorrowerId))
            .ForMember(dest => dest.RequestedOn, opt => opt.MapFrom(src => src.RequestedOn))
            .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
            .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate));

        CreateMap<Data.Item, ItemDto>();

        CreateMap<Data.Booking, BookingDto>()
            .ForMember(dest => dest.Item, opt => opt.MapFrom(src => src.Item))
            .MaxDepth(1);
    }
}