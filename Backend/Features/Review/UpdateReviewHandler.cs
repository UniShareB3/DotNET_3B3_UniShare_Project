using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Review;

public class UpdateReviewHandler(ApplicationContext dbContext) : IRequestHandler<UpdateReviewRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<UpdateReviewHandler>();
    
    public async Task<IResult> Handle(UpdateReviewRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to update review {ReviewId}", request.Id);

        try
        {
            var review = await dbContext.Reviews
                .FindAsync([request.Id], cancellationToken);

            if (review is null)
            {
                _logger.Warning("Update failed: Review {ReviewId} not found.", request.Id);
                return Results.NotFound();
            }

            review.Rating = request.Review.Rating;
            review.Comment = request.Review.Comment;
            
            await dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.Information("Review {ReviewId} updated successfully.", review.Id);
            
            return Results.NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.Error(ex, "Concurrency error while updating review {ReviewId}.", request.Id);
            return Results.Conflict("The review was modified by another user. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error while updating review {ReviewId}", request.Id);
            return Results.InternalServerError();
        }
    }
}