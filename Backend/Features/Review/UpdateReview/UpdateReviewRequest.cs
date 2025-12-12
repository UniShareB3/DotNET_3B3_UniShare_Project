using Backend.Features.Review.DTO;
using MediatR;

namespace Backend.Features.Review;

public class UpdateReviewRequest(Guid reviewId, UpdateReviewDto review) : IRequest<IResult>
{
    public Guid ReviewId { get; } = reviewId;
    public UpdateReviewDto Review { get; } = review;
}
