using Backend.Data;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Handlers.Reviews;

public class CreateReviewHandlerTests
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
    public async Task Given_UserAndItem_When_AddingReview_Then_ReviewIsAdded()
    {
        // Arrange
        var context = CreateInMemoryDbContext("12476839-a33e-4bba-b001-0167bf09e105");
        var userId = Guid.Parse("02476839-a33e-4bba-b001-0165bf09e105");
        var itemId = Guid.Parse("02476839-a33e-4bba-b001-0167b009e105");

        var user = new User
        {
            Id = userId,
            FirstName = "Review",
            LastName = "User",
            Email = "test@student.uaic.ro"
        };
        var item = new Item
        {
            Id = itemId,
            OwnerId = userId,
            Name = "Reviewed Item",
            Description = "Item for review testing",
            Category = Features.Items.Enums.ItemCategory.Books,
            Condition = Features.Items.Enums.ItemCondition.Good
        };

        context.Users.Add(user);
        context.Items.Add(item);
        await context.SaveChangesAsync();
        var review = new Review
        {
            Id = Guid.NewGuid(),
            TargetItemId = itemId,
            ReviewerId = userId,
            Rating = 5,
            Comment = "Excellent item!"
        };

        // Act
        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        // Assert
        var addedReview = await context.Reviews
            .FirstOrDefaultAsync(r => r.Id == review.Id);
        Assert.NotNull(addedReview);
        Assert.Equal(5, addedReview.Rating);
        Assert.Equal("Excellent item!", addedReview.Comment);
    }
}