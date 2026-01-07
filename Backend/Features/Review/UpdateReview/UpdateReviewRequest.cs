using Backend.Features.Review.DTO;
using MediatR;

namespace Backend.Features.Review.UpdateReview;

public record UpdateReviewRequest(Guid ReviewId, UpdateReviewDto Review) : IRequest<IResult>;
