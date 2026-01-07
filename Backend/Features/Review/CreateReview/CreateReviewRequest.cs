using Backend.Features.Review.DTO;
using MediatR;

namespace Backend.Features.Review.CreateReview;

public record CreateReviewRequest(CreateReviewDto Review) : IRequest<IResult>;
