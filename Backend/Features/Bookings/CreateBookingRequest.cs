using Backend.Features.Booking.DTO;
using MediatR;

namespace Backend.Features.Booking;

public record CreateBookingRequest(CreateBookingDto Booking) : IRequest<IResult>;
