using Backend.Features.Bookings.Enums;

namespace Backend.Features.Bookings.DTO;



public record BookingDto(
    Guid Id,
    Guid ItemId,
    Guid BorrowerId,
    DateTime RequestedOn,
    DateTime StartDate,
    DateTime EndDate,
    BookingStatus BookingStatus,
    DateTime? ApprovedOn,
    DateTime? CompletedOn,
    ItemDto? Item
);

