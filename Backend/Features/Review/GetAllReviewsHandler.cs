using Backend.Features.Booking;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Review;

public class GetAllReviewsHandler(ApplicationContext dbContext, ILogger<GetAllReviewsHandler> logger) : IRequestHandler<GetAllReviewsRequest, IResult>
{
    public async Task<IResult> Handle(GetAllReviewsRequest request, CancellationToken cancellationToken)
    {
        var reviews = await dbContext.Reviews.ToListAsync(cancellationToken);
        
        logger.LogInformation("Retrieved {Count} reviews from the database.", reviews.Count);
        return Results.Ok(reviews);
    }
}


