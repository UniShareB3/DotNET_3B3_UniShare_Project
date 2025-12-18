using AutoMapper;
using Backend.Data;
using Backend.Features.Review;
using Backend.Features.Review.DTO;
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
    
    private static IMapper CreateMapper()
    {
        var mapperMock = new Mock<IMapper>();
        mapperMock
            .Setup(m => m.Map<Review>(It.IsAny<CreateReviewDTO>()))
            .Returns((Func<CreateReviewDTO, Review>)(src => new Review
            {
                Id = Guid.Parse("02476839-a33e-4bba-b001-0165bf09e115"),
                BookingId = src.BookingId,
                ReviewerId = src.ReviewerId,
                TargetUserId = src.TargetUserId,
                TargetItemId = src.TargetItemId,
                Rating = src.Rating,
                Comment = src.Comment,
                CreatedAt = src.CreatedAt
            }));

        return mapperMock.Object;
    }
    
    [Fact]
    public async Task Given_ReviewExists_When_Handle_Then_ReturnsOkWithReview()
    {
        // Arrange
        var logger = new Mock<ILogger<GetReviewHandler>>().Object;
        var mapper = CreateMapper();
        var context = CreateInMemoryDbContext("09976839-a33e-4bba-b001-0165bf09e105");
        var reviewId = Guid.Parse("02476839-933e-4bba-b001-0165bf09e105");
        var review = new Review
        {
            Id = reviewId,
            ReviewerId = Guid.Parse("02476839-a33e-4bba-b001-0105bf09e105"),
            TargetUserId = Guid.Parse("02479839-a33e-4bba-b001-0165bf09e105"),
            Rating = 5,
            Comment = "Great experience!"
        };
        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        var handler = new GetReviewHandler(context, mapper ,logger);
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
        var mapper = CreateMapper();
        var context = CreateInMemoryDbContext("02476839-a33e-4bba-b001-0165bf09e105");
        var nonExistentReviewId = Guid.Parse("99976839-a33e-4bba-b001-0165bf09e105");

        var handler = new GetReviewHandler(context, mapper ,logger);
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
        var mapper = CreateMapper();
        var context = CreateInMemoryDbContext("02476839-a33e-4bba-b001-0165bf099105");

        var handler = new GetReviewHandler(context, mapper, logger);
        var request = new GetReviewRequest(Guid.Empty);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}