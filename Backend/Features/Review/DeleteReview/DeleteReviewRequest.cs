using MediatR;

namespace Backend.Features.Review.DeleteReview;

public record DeleteReviewRequest(Guid Id) : IRequest<IResult>;