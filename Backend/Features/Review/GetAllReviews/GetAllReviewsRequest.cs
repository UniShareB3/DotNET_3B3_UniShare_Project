using MediatR;

namespace Backend.Features.Review;

public record GetAllReviewsRequest() : IRequest<IResult>;