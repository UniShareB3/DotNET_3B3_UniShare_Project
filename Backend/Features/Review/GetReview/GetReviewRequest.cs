using MediatR;

namespace Backend.Features.Review;

public record GetReviewRequest(Guid Id) : IRequest<IResult>;