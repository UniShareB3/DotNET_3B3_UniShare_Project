namespace Backend.Features.Bookings.DTO;

public record BookingDto(
    Guid Id,
    Guid ItemId,
    Guid BorrowerId,
    DateTime RequestedOn,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    DateTime? ApprovedOn,
    DateTime? CompletedOn,
    ItemDto? Item
);

