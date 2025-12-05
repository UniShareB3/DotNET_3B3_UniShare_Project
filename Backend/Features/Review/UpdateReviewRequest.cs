using Backend.Features.Review.DTO;
using MediatR;

namespace Backend.Features.Review;

public class UpdateReviewRequest(Guid reviewId, CreateReviewDTO review) : IRequest<IResult>
{
    public Guid ReviewId { get; } = reviewId;
    public CreateReviewDTO Review { get; } = review;
}

