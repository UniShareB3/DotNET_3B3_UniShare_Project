using Backend.Features.Review.DTO;
using MediatR;

namespace Backend.Features.Review;

public record UpdateReviewRequest(Guid Id,UpdateReviewDto Review):IRequest<IResult>;