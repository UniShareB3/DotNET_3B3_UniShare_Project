using Backend.Features.Bookings.DTO;
using MediatR;

namespace Backend.Features.Bookings;

public record CreateBookingRequest(CreateBookingDto Booking) : IRequest<IResult>;
