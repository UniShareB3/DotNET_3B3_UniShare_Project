using AutoMapper;
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
        _logger.Information("Updating review {ReviewId}", request.ReviewId);

        var existingReview = await dbContext.Reviews
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId, cancellationToken);

        if (existingReview == null)
        {
            _logger.Warning("Review {ReviewId} not found", request.ReviewId);
            return Results.NotFound(new { message = "Review not found" });
        }

        // Update only allowed fields (rating and comment)
        existingReview.Rating = request.Review.Rating;
        existingReview.Comment = request.Review.Comment;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.Information("Review {ReviewId} updated successfully", request.ReviewId);

            return Results.Ok(existingReview);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error while updating review {ReviewId}", request.ReviewId);
            return Results.StatusCode(500);
        }
    }
}

