using Backend.Data;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Handlers.Reviews;

public class GetAllReviewsHandlerTests
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
    public async Task Given_ReviewsExist_When_GettingAllReviews_Then_ReturnsAllReviews()
    {
        // Arrange
        var context = CreateInMemoryDbContext("02776839-a33e-4bba-b001-0167bf09e1a5");
        
        var review1 = new Review
        {
            Id = Guid.Parse("02776839-a33e-4bba-b001-0167bf09e1b5"),
            TargetItemId = Guid.Parse("02476839-a33e-4bba-b001-0167bf09e1b5"),
            ReviewerId = Guid.Parse("01776839-a33e-4bba-b001-0167bf09e1b5"),
            Rating = 5,
            Comment = "Great item!"
        };
        
        var review2 = new Review
        {
            Id = Guid.Parse("02776839-a33e-4bba-b002-0167bf09e1b5"),
            TargetItemId = Guid.Parse("02775839-a33e-4bba-b001-0167bf09e1b5"),
            ReviewerId = Guid.Parse("02776839-a33e-4bba-b001-0167b009e1b5"),
            Rating = 4,
            Comment = "Good quality."
        };
        
        context.Reviews.AddRange(review1, review2);
        await context.SaveChangesAsync();
        
        // Act
        var retrievedReviews = await context.Reviews.ToListAsync();
        
        // Assert
        Assert.Equal(2, retrievedReviews.Count);
        Assert.Contains(retrievedReviews, r => r.Id == review1.Id);
        Assert.Contains(retrievedReviews, r => r.Id == review2.Id);
    }
    
    [Fact]
    public async Task Given_NoReviewsExist_When_GettingAllReviews_Then_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateInMemoryDbContext("02776839-a33e-4bba-b001-0167bf09e1c6");
        
        // Act
        var retrievedReviews = await context.Reviews.ToListAsync();
        
        // Assert
        Assert.Empty(retrievedReviews);
    }
}