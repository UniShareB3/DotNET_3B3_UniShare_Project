using AutoMapper;
using AutoMapper.QueryableExtensions;
using Backend.Features.Review.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Review;

public class GetAllReviewsHandler(ApplicationContext dbContext, IMapper mapper, ILogger<GetAllReviewsHandler> logger) : IRequestHandler<GetAllReviewsRequest, IResult>
{
    public async Task<IResult> Handle(GetAllReviewsRequest request, CancellationToken cancellationToken)
    {
        var reviews = await dbContext.Reviews
            .ProjectTo<ReviewDto>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        
        logger.LogInformation("Retrieved {Count} reviews from the database.", reviews.Count);
        return Results.Ok(reviews);
    }
}


