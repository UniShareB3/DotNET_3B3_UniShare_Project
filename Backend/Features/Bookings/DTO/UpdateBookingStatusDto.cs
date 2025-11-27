namespace Backend.Features.Bookings.DTO;

public record UpdateBookingStatusDto(Guid UserId, string Status);
