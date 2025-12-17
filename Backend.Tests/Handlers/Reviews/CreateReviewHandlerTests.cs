using AutoMapper;
using Backend.Data;
using Backend.Features.Review;
using Backend.Features.Review.DTO;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

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
    public async Task Given_ValidReviewRequest_When_Handle_Then_ReviewIsCreated()
    {
        // Arrange
        var context = CreateInMemoryDbContext("create-review-handler-test-" + Guid.NewGuid());
        var mapper = CreateMapper();
        var handler = new CreateReviewHandler(context, mapper);
        
        var userId = Guid.Parse("02476839-a33e-4bba-b001-0165bf09e105");
        var itemId = Guid.Parse("02476839-a33e-4bba-b001-0167b009e105");
        var bookingId = Guid.NewGuid();

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

        var reviewDto = new CreateReviewDTO(
            BookingId: bookingId,
            ReviewerId: userId,
            TargetUserId: null,
            TargetItemId: itemId,
            Rating: 5,
            Comment: "Excellent item!",
            CreatedAt: DateTime.UtcNow
        );
        
        var request = new CreateReviewRequest(reviewDto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task Given_Exception_When_Handle_Then_ReturnsInternalServerError()
    {
        // Arrange
        var mapper = CreateMapper();

        var context = CreateInMemoryDbContext("create-review-exception-test-" + Guid.NewGuid());
        var handler = new CreateReviewHandler(context, mapper);

        var reviewDto = new CreateReviewDTO(
            BookingId: Guid.NewGuid(),
            ReviewerId: Guid.NewGuid(),
            TargetUserId: null,
            TargetItemId: Guid.NewGuid(),
            Rating: 4,
            Comment: "Good item.",
            CreatedAt: DateTime.UtcNow
        );
        
        var request = new CreateReviewRequest(reviewDto);

        await context.DisposeAsync();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}