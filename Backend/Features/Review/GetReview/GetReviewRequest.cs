using MediatR;

namespace Backend.Features.Review.GetReview;

public record GetReviewRequest(Guid Id) : IRequest<IResult>;