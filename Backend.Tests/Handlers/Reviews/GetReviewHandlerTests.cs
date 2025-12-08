using Backend.Data;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Handlers.Reviews;

public class GetReviewHandlerTests
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
    public async Task Given_ReviewExists_When_GettingReview_Then_ReturnsReview()
    {
        // Arrange
        var context = CreateInMemoryDbContext("review-test-db");
        var reviewId = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var review = new Review
        {
            Id = reviewId,
            ReviewerId = Guid.Parse("12345678-1234-1234-1234-1234567890ac"),
            TargetUserId = Guid.Parse("12345678-1234-1234-1234-1234567890ad"),
            Rating = 5,
            Comment = "Great experience!"
        };
        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        // Act
        var retrievedReview = await context.Reviews.FindAsync(reviewId);

        // Assert
        Assert.NotNull(retrievedReview);
        Assert.Equal(reviewId, retrievedReview.Id);
        Assert.Equal(5, retrievedReview.Rating);
        Assert.Equal("Great experience!", retrievedReview.Comment);
    }
    
    [Fact]
    public async Task Given_ReviewDoesNotExist_When_GettingReview_Then_ReturnsNull()
    {
        // Arrange
        var context = CreateInMemoryDbContext("review-nonexistent-test-db");
        var nonExistentReviewId = Guid.NewGuid();

        // Act
        var retrievedReview = await context.Reviews.FindAsync(nonExistentReviewId);

        // Assert
        Assert.Null(retrievedReview);
    }
}