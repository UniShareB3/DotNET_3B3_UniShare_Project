using MediatR;

namespace Backend.Features.Items.GetBookingForItem
{
    public record GetBookingsForItemRequest(Guid ItemId) : IRequest<IResult>;
}

