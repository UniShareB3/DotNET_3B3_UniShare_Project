using Backend.Data;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;

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
    public async Task Given_ReviewExists_When_DeletingReview_Then_RemovesReview()
    {
        // Arrange
        var context = CreateInMemoryDbContext("02476119-a33e-422a-b001-0167bf09e1b5");
        var reviewId = Guid.Parse("02476839-a33e-4bba-b001-0167bf090005");
        var review = new Review
        {
            Id = reviewId,
            ReviewerId = Guid.Parse("02476839-a33e-4bba-b001-0167bf09e105"),
            TargetItemId = Guid.Parse("02476839-a33e-4bba-b001-0167bf09e1b0"),
            Rating = 5,
            Comment = "Great item!"
        };
        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        // Act
        var reviewToDelete = await context.Reviews.FindAsync(reviewId);
        if (reviewToDelete != null)
        {
            context.Reviews.Remove(reviewToDelete);
            await context.SaveChangesAsync();
        } 

        // Assert
        var deletedReview = await context.Reviews.FindAsync(reviewId);
        Assert.Null(deletedReview);
    }
    
    [Fact]
    public async Task Given_ReviewDoesNotExist_When_DeletingReview_Then_NoActionTaken()
    {
        // Arrange
        var context = CreateInMemoryDbContext("delete-nonexistent-review-test-db");
        var nonExistentReviewId = Guid.Parse("99999999-8888-7777-6666-555555555555");

        // Act
        var reviewToDelete = await context.Reviews.FindAsync(nonExistentReviewId);
        if (reviewToDelete != null)
        {
            context.Reviews.Remove(reviewToDelete);
            await context.SaveChangesAsync();
        } 

        // Assert
        var reviewsCount = await context.Reviews.CountAsync();
        Assert.Equal(0, reviewsCount);
    }
    
    [Fact]
    public async Task Given_MultipleReviewsExist_When_DeletingOneReview_Then_OtherReviewsRemain()
    {
        // Arrange
        var context = CreateInMemoryDbContext("02470039-a33e-422a-b001-0167bf09e1b5");
        var review1 = new Review
        {
            Id = Guid.Parse("02476839-a33e-422a-b001-0167bf09e1b5"),
            ReviewerId = Guid.Parse("02476839-a11e-4bba-b001-0167bf09e1b5"),
            TargetItemId = Guid.Parse("02476839-a33e-4bba-1001-0167bf09e1b5"),
            Rating = 4,
            Comment = "Good item."
        };
        var review2 = new Review
        {
            Id = Guid.Parse("02476839-a33e-4bba-b001-0167bf09e1b0"),
            ReviewerId = Guid.Parse("02406839-a33e-4bba-b001-0167bf09e1b5"),
            TargetItemId = Guid.Parse("02476839-a30e-4bba-b001-0167bf09e1b5"),
            Rating = 2,
            Comment = "Not as expected."
        };
        context.Reviews.AddRange(review1, review2);
        await context.SaveChangesAsync();

        // Act
        var reviewToDelete = await context.Reviews.FindAsync(review1.Id);
        if (reviewToDelete != null)
        {
            context.Reviews.Remove(reviewToDelete);
            await context.SaveChangesAsync();
        } 

        // Assert
        var remainingReview = await context.Reviews.FindAsync(review2.Id);
        Assert.NotNull(remainingReview);
        Assert.Equal(review2.Comment, remainingReview.Comment);
    }
    
    [Fact]
    public async Task Given_ReviewExists_When_DeletingReviewTwice_Then_NoErrorOccurs()
    {
        // Arrange
        var context = CreateInMemoryDbContext("02476839-a33e-422a-0001-0167bf09e1b5");
        var reviewId = Guid.Parse("02476839-a33e-4bba-b001-0167bf09e1b7");
        var review = new Review
        {
            Id = reviewId,
            ReviewerId = Guid.Parse("02476839-a33e-4bba-b001-0167bf09e105"),
            TargetItemId = Guid.Parse("02476839-a33e-4bba-b001-0167bf09e1b0"),
            Rating = 3,
            Comment = "Average item."
        };
        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        // Act
        var reviewToDelete = await context.Reviews.FindAsync(reviewId);
        if (reviewToDelete != null)
        {
            context.Reviews.Remove(reviewToDelete);
            await context.SaveChangesAsync();
        } 

        // Attempt to delete again
        reviewToDelete = await context.Reviews.FindAsync(reviewId);
        if (reviewToDelete != null)
        {
            context.Reviews.Remove(reviewToDelete);
            await context.SaveChangesAsync();
        } 

        // Assert
        var deletedReview = await context.Reviews.FindAsync(reviewId);
        Assert.Null(deletedReview);
    }
}