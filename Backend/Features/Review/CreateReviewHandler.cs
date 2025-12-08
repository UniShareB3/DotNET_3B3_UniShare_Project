using AutoMapper;
using Backend.Features.Bookings;
using Backend.Persistence;
using MediatR;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Review;

public class CreateReviewHandler(ApplicationContext dbContext, IMapper mapper) : IRequestHandler<CreateReviewRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<CreateBookingHandler>();
    
    public async Task<IResult> Handle(CreateReviewRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Creating review for item {TargetItemId} and targetUser {TargetUserId}", request.Review.TargetItemId, request.Review.TargetUserId);

        var review = mapper.Map<Data.Review>(request.Review);
        
        if (review.Id == Guid.Empty)
        {
            review.Id = Guid.NewGuid();
        }

        try
        {
            dbContext.Reviews.Add(review);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.Information("Review {ReviewId} created successfully for item {TargetItemId} and targetUser {TargetUserId}",
                review.Id, review.TargetItemId, review.TargetUserId);
                
            return Results.Created($"/reviews/{review.Id}", review);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error while creating review for item {TargetItemId} and targetUser {TargetUserId}", request.Review.TargetItemId, request.Review.TargetUserId);
            return Results.StatusCode(500);
        }
    }
}