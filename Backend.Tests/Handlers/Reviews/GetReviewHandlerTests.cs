using Backend.Data;
using Backend.Features.Review;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

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
    public async Task Given_ReviewExists_When_Handle_Then_ReturnsOkWithReview()
    {
        // Arrange
        var logger = new Mock<ILogger<GetReviewHandler>>().Object;
        var context = CreateInMemoryDbContext("get-review-exists-test-" + Guid.NewGuid());
        var reviewId = Guid.NewGuid();
        var review = new Review
        {
            Id = reviewId,
            ReviewerId = Guid.NewGuid(),
            TargetUserId = Guid.NewGuid(),
            Rating = 5,
            Comment = "Great experience!"
        };
        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        var handler = new GetReviewHandler(context, logger);
        var request = new GetReviewRequest(reviewId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
    
    [Fact]
    public async Task Given_ReviewDoesNotExist_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var logger = new Mock<ILogger<GetReviewHandler>>().Object;
        var context = CreateInMemoryDbContext("get-review-not-found-test-" + Guid.NewGuid());
        var nonExistentReviewId = Guid.NewGuid();

        var handler = new GetReviewHandler(context, logger);
        var request = new GetReviewRequest(nonExistentReviewId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
    
    [Fact]
    public async Task Given_EmptyGuidReviewId_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var logger = new Mock<ILogger<GetReviewHandler>>().Object;
        var context = CreateInMemoryDbContext("get-review-empty-guid-test-" + Guid.NewGuid());

        var handler = new GetReviewHandler(context, logger);
        var request = new GetReviewRequest(Guid.Empty);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}