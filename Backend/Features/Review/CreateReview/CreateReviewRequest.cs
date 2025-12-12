using Backend.Features.Review.DTO;
using MediatR;

namespace Backend.Features.Review;

public record CreateReviewRequest(CreateReviewDTO Review) : IRequest<IResult>;
