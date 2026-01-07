using Backend.Data;
using Backend.Features.Review.DeleteReview;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Handlers.Reviews;

public class DeleteReviewHandlerTests
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
    public async Task Given_ReviewExists_When_Handle_Then_DeletesReviewAndReturnsOk()
    {
        // Arrange
        var logger = new Mock<ILogger<DeleteReviewHandler>>().Object;
        var context = CreateInMemoryDbContext("11476839-a33e-4bba-b001-0165bf09e105");
        var reviewId = Guid.Parse("02476839-a33e-4bba-b001-0165bf09e105");
        var review = new Review
        {
            Id = reviewId,
            ReviewerId = Guid.Parse("11476839-a33e-4bba-b001-0165bf09e105"),
            TargetItemId = Guid.Parse("02276839-a33e-4bba-b001-0165bf09e105"),
            Rating = 5,
            Comment = "Great item!"
        };
        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        var handler = new DeleteReviewHandler(context, logger);
        var request = new DeleteReviewRequest(reviewId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var deletedReview = await context.Reviews.FindAsync(reviewId);
        deletedReview.Should().BeNull();
    }
    
    [Fact]
    public async Task Given_ReviewDoesNotExist_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var logger = new Mock<ILogger<DeleteReviewHandler>>().Object;
        var context = CreateInMemoryDbContext("02476839-a33e-4bba-6001-0165bf09e105");
        var nonExistentReviewId = Guid.Parse("02176839-a33e-4bba-b001-0165bf09e105");

        var handler = new DeleteReviewHandler(context, logger);
        var request = new DeleteReviewRequest(nonExistentReviewId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
    
    [Fact]
    public async Task Given_MultipleReviewsExist_When_Handle_Then_DeletesOnlyTargetReview()
    {
        // Arrange
        var logger = new Mock<ILogger<DeleteReviewHandler>>().Object;
        var context = CreateInMemoryDbContext("02476839-113e-4bba-b001-0165bf09e105");
        var review1Id = Guid.Parse("02476839-a13e-4bba-b001-0165bf09e105");
        var review2Id = Guid.Parse("02476819-a33e-4bba-b001-0165bf09e105");
        
        var review1 = new Review
        {
            Id = review1Id,
            ReviewerId = Guid.Parse("02476839-a33e-4b11-b001-0165bf09e105"),
            TargetItemId = Guid.Parse("01176839-a33e-4bba-b001-0165bf09e105"),
            Rating = 4,
            Comment = "Good item."
        };
        var review2 = new Review
        {
            Id = review2Id,
            ReviewerId = Guid.Parse("02471139-a33e-4bba-b001-0165bf09e105"),
            TargetItemId = Guid.Parse("02476119-a33e-4bba-b001-0165bf09e105"),
            Rating = 2,
            Comment = "Not as expected."
        };
        context.Reviews.AddRange(review1, review2);
        await context.SaveChangesAsync();

        var handler = new DeleteReviewHandler(context, logger);
        var request = new DeleteReviewRequest(review1Id);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var deletedReview = await context.Reviews.FindAsync(review1Id);
        deletedReview.Should().BeNull();
        
        var remainingReview = await context.Reviews.FindAsync(review2Id);
        remainingReview.Should().NotBeNull();
        remainingReview.Comment.Should().Be("Not as expected.");
    }
    
    [Fact]
    public async Task Given_ReviewDeleted_When_HandleCalledAgain_Then_ReturnsNotFound()
    {
        // Arrange
        var logger = new Mock<ILogger<DeleteReviewHandler>>().Object;
        var context = CreateInMemoryDbContext("02476839-1331-4bba-b001-0165bf09e105");
        var reviewId = Guid.Parse("02436839-a33e-4bba-b001-0165bf09e105");
        var review = new Review
        {
            Id = reviewId,
            ReviewerId = Guid.Parse("02446839-a33e-4bba-b001-0165bf09e105"),
            TargetItemId = Guid.Parse("02476839-a35e-4bba-b001-0165bf09e105"),
            Rating = 3,
            Comment = "Average item."
        };
        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        var handler = new DeleteReviewHandler(context, logger);
        var request = new DeleteReviewRequest(reviewId);

        // Act - First delete
        var firstResult = await handler.Handle(request, CancellationToken.None);
        
        // Assert first delete succeeds
        var firstStatusResult = firstResult.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        firstStatusResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        // Act - Second delete 
        var secondResult = await handler.Handle(request, CancellationToken.None);

        // Assert second delete returns NotFound
        var secondStatusResult = secondResult.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        secondStatusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
    
    [Fact]
    public async Task Given_EmptyGuidReviewId_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var logger = new Mock<ILogger<DeleteReviewHandler>>().Object;
        var context = CreateInMemoryDbContext("delete-empty-guid-test-" + Guid.NewGuid());

        var handler = new DeleteReviewHandler(context, logger);
        var request = new DeleteReviewRequest(Guid.Empty);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}