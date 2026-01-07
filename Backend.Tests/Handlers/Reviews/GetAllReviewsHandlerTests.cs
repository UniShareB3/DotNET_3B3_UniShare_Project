using AutoMapper;
using Backend.Data;
using Backend.Features.Review;
using Backend.Features.Review.GetAllReviews;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

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

    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Review, Backend.Features.Review.DTO.ReviewDto>();
        }, new LoggerFactory());
        return config.CreateMapper();
    }

    [Fact]
    public async Task Given_ReviewsExist_When_Handle_Then_ReturnsOkWithAllReviews()
    {
        // Arrange
        var logger = new Mock<ILogger<GetAllReviewsHandler>>().Object;
        var context = CreateInMemoryDbContext("02476839-a33e-4bba-b001-0165bf09e105");
        var mapper = CreateMapper();
        
        var review1 = new Review
        {
            Id = Guid.Parse("02476839-a33e-4bba-b001-0165bf09e105"),
            TargetItemId = Guid.Parse("02476819-a33e-4bba-b001-0165bf09e105"),
            ReviewerId =Guid.Parse("02476839-a33e-4bba-b001-1165bf09e105"),
            Rating = 5,
            Comment = "Great item!"
        };
        
        var review2 = new Review
        {
            Id = Guid.Parse("02476839-a11e-4bba-b001-0165bf09e105"),
            TargetItemId = Guid.Parse("01116839-a33e-4bba-b001-0165bf09e105"),
            ReviewerId = Guid.Parse("02476839-a33e-1111-b001-0165bf09e105"),
            Rating = 4,
            Comment = "Good quality."
        };
        
        context.Reviews.AddRange(review1, review2);
        await context.SaveChangesAsync();
        
        var handler = new GetAllReviewsHandler(context, mapper, logger);
        var request = new GetAllReviewsRequest();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
    
    [Fact]
    public async Task Given_NoReviewsExist_When_Handle_Then_ReturnsOkWithEmptyList()
    {
        // Arrange
        var logger = new Mock<ILogger<GetAllReviewsHandler>>().Object;
        var mapper = CreateMapper();
        var context = CreateInMemoryDbContext("get-all-reviews-empty-test-" + Guid.NewGuid());
        
        var handler = new GetAllReviewsHandler(context, mapper ,logger);
        var request = new GetAllReviewsRequest();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}