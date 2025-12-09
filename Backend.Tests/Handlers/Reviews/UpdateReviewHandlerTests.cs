using Backend.Data;
using Backend.Features.Review;
using Backend.Features.Review.DTO;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Handlers.Reviews;

public class UpdateReviewHandlerTests
{
    private static ApplicationContext CreateInMemoryDbContext(string guid)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: guid)
            .Options;

        var context = new ApplicationContext(options);
        return context;
    }
    
    [Fact]
    public async Task Given_ReviewExists_When_Handle_Then_UpdatesReview()
    {
        // Arrange
        var context = CreateInMemoryDbContext("update-review-test-" + Guid.NewGuid());
        var reviewId = Guid.NewGuid();
        var review = new Review
        {
            Id = reviewId,
            ReviewerId = Guid.NewGuid(),
            TargetUserId = Guid.NewGuid(),
            Rating = 3,
            Comment = "Average experience."
        };
        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        var handler = new UpdateReviewHandler(context);
        var updatedRating = 5;
        var updatedComment = "Excellent experience!";
        var dto = new UpdateReviewDto( updatedRating, updatedComment);
        var request = new UpdateReviewRequest(reviewId, dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        
        var updatedReview = await context.Reviews.FindAsync(reviewId);
        updatedReview!.Rating.Should().Be(updatedRating);
        updatedReview.Comment.Should().Be(updatedComment);
    }
    
    [Fact]
    public async Task Given_ReviewDoesNotExist_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext("update-review-not-found-test-" + Guid.NewGuid());
        var handler = new UpdateReviewHandler(context);
        var nonExistentReviewId = Guid.NewGuid();
        var dto = new UpdateReviewDto(4, "Good experience.");
        var request = new UpdateReviewRequest(nonExistentReviewId, dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
    
    [Fact]
    public async Task Give_ExceptionOccurs_When_Handle_Then_ReturnsProblemResult()
    {
        // Arrange
        var context = CreateInMemoryDbContext("update-review-exception-test-" + Guid.NewGuid());
        var reviewId = Guid.NewGuid();
        var review = new Review
        {
            Id = reviewId,
            ReviewerId = Guid.NewGuid(),
            TargetUserId = Guid.NewGuid(),
            Rating = 2,
            Comment = "Not great."
        };
        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        var handler = new UpdateReviewHandler(context);
        var dto = new UpdateReviewDto(5, "Nice!");
        var request = new UpdateReviewRequest(reviewId, dto);
        
        context.Dispose();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}