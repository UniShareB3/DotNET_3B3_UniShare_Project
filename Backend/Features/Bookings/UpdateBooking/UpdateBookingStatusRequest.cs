using Backend.Features.Bookings.DTO;
using MediatR;

namespace Backend.Features.Bookings;

public record UpdateBookingStatusRequest(Guid BookingId, UpdateBookingStatusDto BookingStatusDto) : IRequest<IResult>;
