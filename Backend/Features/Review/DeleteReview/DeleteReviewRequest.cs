using MediatR;

namespace Backend.Features.Review;

public record DeleteReviewRequest(Guid Id) : IRequest<IResult>;