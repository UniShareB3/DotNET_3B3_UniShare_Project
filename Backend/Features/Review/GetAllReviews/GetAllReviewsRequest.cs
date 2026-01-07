using MediatR;

namespace Backend.Features.Review.GetAllReviews;

public record GetAllReviewsRequest() : IRequest<IResult>;