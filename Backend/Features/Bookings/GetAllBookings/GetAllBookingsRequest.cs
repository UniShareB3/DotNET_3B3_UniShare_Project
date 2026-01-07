using MediatR;

namespace Backend.Features.Bookings.GetAllBookings;

public record GetAllBookingsRequest : IRequest<IResult>;