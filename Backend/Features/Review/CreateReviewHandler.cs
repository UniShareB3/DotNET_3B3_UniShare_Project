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
    
    public Task<IResult> Handle(CreateReviewRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Creating review for item {TargetItemId} and targetUser {}", request.Review.TargetItemId, request.Review.TargetUserId);
        
        var review = mapper.Map<Data.Review>(request.Review);
        
        if (review.Id == Guid.Empty)
        {
            review.Id = Guid.NewGuid();
        }

        try
        {
            dbContext.Reviews.Add(review);
            dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.Information("Review {ReviewId} created successfully for item {TargetItemId} and targetUser {}", 
                review.Id, review.TargetItemId, review.TargetUserId);
                
            return new Task<IResult>( () => Results.Created($"/reviews/{review.Id}", review));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error while creating review for item {TargetItemId} and targetUser {}", request.Review.TargetItemId, request.Review.TargetUserId);
            return new Task<IResult>( Results.InternalServerError );
        }
    }
}