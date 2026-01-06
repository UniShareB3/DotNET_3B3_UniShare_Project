using AutoMapper;
using Backend.Data;
using Backend.Features.Items;
using Backend.Features.Items.DTO;
using Backend.Features.Items.Enums;
using Backend.Features.Items.PostItem;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Backend.Tests.Handlers.Items;

public class PostItemHandlerTests
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
            .Setup(m => m.Map<Item>(It.IsAny<PostItemDto>()))
            .Returns((Func<PostItemDto, Item>)(src => new Item
            {
                OwnerId = src.OwnerId,
                Name = src.Name,
                Description = src.Description,
                Category = Enum.Parse<ItemCategory>(src.Category),
                Condition = Enum.Parse<ItemCondition>(src.Condition),
                ImageUrl = src.ImageUrl
            }));

        return mapperMock.Object;
    }

    [Fact]
    public async Task Given_ValidPostItemRequest_When_Handle_Then_AddsNewItem()
    {
        // Arrange
        var context = CreateInMemoryDbContext("a81a22fd-7df5-4d65-a0b5-aec7fa7dc5a3");
        var ownerId = Guid.Parse("a81a22fd-7df5-4d65-a0b5-aec7fa7dc5a3");
        
        // Add the owner user to the database
        var user = new User 
        { 
            Id = ownerId, 
            FirstName = "Test", 
            LastName = "User", 
            Email = "testuser@example.com" 
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        var handler = new PostItemHandler(context, CreateMapper());
        var dto = new PostItemDto (
            ownerId,
            "Test Item",
            "This is a test item.",
            "Electronics",
            "0",
            "http://example.com/image.jpg"
        );

        // Act
        var result = await handler.Handle(new PostItemRequest(dto), CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        
        result.Should().NotBeNull();
        var createItem = await context.Items.FirstOrDefaultAsync( item => item.Name == "Test Item");
        Assert.NotNull(createItem);
        Assert.Equal("This is a test item.", createItem.Description);
        Assert.Equal(ItemCategory.Electronics, createItem.Category);
        Assert.Equal(ItemCondition.New, createItem.Condition);
        Assert.Equal("http://example.com/image.jpg", createItem.ImageUrl);
    }

    
    [Fact]
    public async Task Given_PostItemRequestWithNonExistentOwner_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext("b91b22fd-7df5-4d65-a0b5-aec7fa7dc5b4");
        var handler = new PostItemHandler(context, CreateMapper());
        var nonExistentOwnerId = Guid.Parse("b91b22fd-7df5-4d65-a0b5-aec7fa7dc5b4");
        var dto = new PostItemDto (
            nonExistentOwnerId,
            "Test Item",
            "This is a test item.",
            "Electronics",
            "0",
            "http://example.com/image.jpg"
        );

        // Act
        var result = await handler.Handle(new PostItemRequest(dto), CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task Given_PostItemRequest_When_Handle_Then_HandlesExceptionGracefully()
    {
        // Arrange
        var context = CreateInMemoryDbContext("b91b22fd-7df5-4d65-a0b5-aec7fa7dc5b4");
        var mapper = CreateMapper();
        var handler = new PostItemHandler(context, mapper);
        var dto = new PostItemDto (
            Guid.NewGuid(),
            "Test Item",
            "This is a test item.",
            "Electronics",
            "0",
            "http://example.com/image.jpg"
        );

        await context.DisposeAsync();

        // Act
        var result = await handler.Handle(new PostItemRequest(dto), CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        
        result.Should().NotBeNull();
    }

}