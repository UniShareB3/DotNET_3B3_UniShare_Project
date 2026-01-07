using AutoMapper;
using Backend.Data;
using Backend.Features.Review;
using Backend.Features.Review.DTO;
using Backend.Features.Review.UpdateReview;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Review, ReviewDto>();
        }, new LoggerFactory());
        return config.CreateMapper();
    }
    
    [Fact]
    public async Task Given_ReviewExists_When_Handle_Then_UpdatesReview()
    {
        // Arrange
        var mapper = CreateMapper();
        var context = CreateInMemoryDbContext("02476839-a33e-4bba-b001-0165bf09e105");
        var reviewId = Guid.Parse("02476839-a33e-4bba-b001-0165bf09e101");
        var review = new Review
        {
            Id = reviewId,
            ReviewerId = Guid.Parse("02476839-a33e-4bba-b001-0165bf091105"),
            TargetUserId = Guid.Parse("02476839-a33e-4bb1-b001-0165bf09e105"),
            Rating = 3,
            Comment = "Average experience."
        };
        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        var handler = new UpdateReviewHandler(context,mapper);
        var updatedRating = 5;
        var updatedComment = "Excellent experience!";
        var dto = new UpdateReviewDto( updatedRating, updatedComment);
        var request = new UpdateReviewRequest(reviewId, dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        (statusResult.StatusCode == StatusCodes.Status204NoContent || statusResult.StatusCode == StatusCodes.Status200OK).Should().BeTrue();

        var updatedReview = await context.Reviews.FindAsync(reviewId);
        updatedReview!.Rating.Should().Be(updatedRating);
        updatedReview.Comment.Should().Be(updatedComment);
    }
    
    [Fact]
    public async Task Given_ReviewDoesNotExist_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext("02476839-a33e-4bba-b001-0165bf09e111");
        var mapper = CreateMapper();
        
        var handler = new UpdateReviewHandler(context,mapper);
        var nonExistentReviewId = Guid.Parse("02476839-a33e-4bba-1001-0165bf09e105");
        var dto = new UpdateReviewDto(4, "Good experience.");
        var request = new UpdateReviewRequest(nonExistentReviewId, dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}