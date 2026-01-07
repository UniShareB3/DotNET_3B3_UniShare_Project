using Backend.Persistence;
using MediatR;

namespace Backend.Features.Review.DeleteReview;

public class DeleteReviewHandler(ApplicationContext dbContext, ILogger<DeleteReviewHandler> logger) : IRequestHandler<DeleteReviewRequest, IResult>
{
    public async Task<IResult> Handle(DeleteReviewRequest request, CancellationToken cancellationToken)
    {
        
        var review = await dbContext.Reviews.FindAsync(request.Id, cancellationToken);
        
        if (review == null)
        {
            logger.LogWarning("Review with ID {ReviewId} not found.", request.Id);
            return Results.NotFound($"Review with ID {request.Id} not found.");
        }

        dbContext.Reviews.Remove(review);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("Deleted review with ID {ReviewId} from the database.", request.Id);
        return Results.Ok($"Review {request.Id} deleted successfully.");
    }
}

