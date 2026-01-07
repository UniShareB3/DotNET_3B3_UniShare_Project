using AutoMapper;
using Backend.Features.Review.DTO;
using Backend.Persistence;
using MediatR;

namespace Backend.Features.Review.GetReview;

public class GetReviewHandler(ApplicationContext dbContext, IMapper mapper, ILogger<GetReviewHandler> logger) : IRequestHandler<GetReviewRequest, IResult>
{
    public async Task<IResult> Handle(GetReviewRequest request, CancellationToken cancellationToken)
    {
        var review = await dbContext.Reviews.FindAsync([request.Id, cancellationToken], cancellationToken: cancellationToken);
        
        if (review == null)
        {
            logger.LogWarning("Review with ID {ReviewId} not found.", request.Id);
            return Results.NotFound($"Review with ID {request.Id} not found.");
        }

        logger.LogInformation("Retrieved review with ID {ReviewId} from the database.", request.Id);
        
        var reviewDto = mapper.Map<ReviewDto>(review);
        return Results.Ok(reviewDto);
    }
}