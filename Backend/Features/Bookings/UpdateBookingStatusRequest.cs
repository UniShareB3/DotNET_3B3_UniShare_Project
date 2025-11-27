namespace Backend.Features.Bookings;

public record UpdateBookingStatusRequest(Guid BookingId, Guid UserId);