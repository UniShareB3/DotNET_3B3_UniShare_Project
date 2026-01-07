using Backend.Features.Bookings.DTO;
using MediatR;

namespace Backend.Features.Bookings.CreateBooking;

public record CreateBookingRequest(CreateBookingDto Booking) : IRequest<IResult>;
