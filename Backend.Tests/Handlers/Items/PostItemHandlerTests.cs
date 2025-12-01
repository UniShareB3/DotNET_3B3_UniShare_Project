using AutoMapper;
using Backend.Data;
using Backend.Features.Items;
using Backend.Features.Items.DTO;
using Backend.Features.Items.Enums;
using Backend.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Backend.Tests.Handlers.Items;

public class PostItemHandlerTests
{
    private static ApplicationContext CreateInMemoryDbContext(string guid)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: guid)
            .Options;

        var dbContext = new ApplicationContext(options);
        return dbContext;
    }
    
    private static Mock<IMapper> CreateMapperMock()
    {
        // Create a mapper mock that maps PostItemDto -> Item and Item -> PostItemDto
        var mapperMock = new Mock<IMapper>();

        mapperMock.Setup(m => m.Map<Item>(It.IsAny<PostItemDto>()))
            .Returns((PostItemDto src) =>
            {
                // If required fields are null, preserve nulls so tests can observe failures
                Enum.TryParse<ItemCategory>(src.Category ?? string.Empty, true, out var category);
                Enum.TryParse<ItemCondition>(src.Condition ?? string.Empty, true, out var condition);

                return new Item
                {
                    OwnerId = src.OwnerId,
                    Name = src.Name,
                    Description = src.Description,
                    Category = category,
                    Condition = condition,
                    ImageUrl = src.ImageUrl
                };
            });

        mapperMock.Setup(m => m.Map<PostItemDto>(It.IsAny<Item>()))
            .Returns((Item src) =>
            {
                return new PostItemDto(
                    src.OwnerId,
                    src.Name,
                    src.Description,
                    src.Category.ToString(),
                    src.Condition.ToString(),
                    src.ImageUrl
                );
            });

        return mapperMock;
    }
    
    [Fact]
    public async Task Given_ValidPostItemRequest_When_Handle_Then_AddsNewItem()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext("a81a22fd-7df5-4d65-a0b5-aec7fa7dc5a3");
        var dto = new PostItemDto (
            Guid.NewGuid(),
            "Test Item",
            "This is a test item.",
            "Electronics",
            "New",
            "http://example.com/image.jpg"
        );

        var item = new Item()
        {
            OwnerId = dto.OwnerId,
            Name = dto.Name,
            Description = dto.Description,
            Category = ItemCategory.Electronics,
            Condition = ItemCondition.New,
            ImageUrl = dto.ImageUrl
        };
        
        Mock<IMapper> mapperMock = CreateMapperMock();
        mapperMock.Setup(m => m.Map<Item>(It.IsAny<PostItemDto>()))
            .Returns(item);
        
        mapperMock.Setup(m => m.Map<PostItemDto>(It.IsAny<Item>()))
            .Returns(dto);

        var handler = new PostItemHandler(dbContext, mapperMock.Object);
        
        // Act
        var result = await handler.Handle(new PostItemRequest(dto), CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        
        result.Should().NotBeNull();
        var createItem = await dbContext.Items.FirstOrDefaultAsync( i => i.Name == "Test Item");
        Assert.NotNull(createItem);
        Assert.Equal("This is a test item.", createItem.Description);
        Assert.Equal(ItemCategory.Electronics, createItem.Category);
        Assert.Equal(ItemCondition.New, createItem.Condition);
        Assert.Equal("http://example.com/image.jpg", createItem.ImageUrl);
    }

    [Fact]
    public async Task Given_PostItemRequest_With_MissingRequiredField_When_Handle_Then_ThrowsDbUpdateException()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext("a7870f45-b0fb-4185-b2cc-50f982d10021");
        Mock<IMapper> mapperMock = CreateMapperMock();
        
        var handler = new PostItemHandler(dbContext, mapperMock.Object);
        
        var dto = new PostItemDto
        (
            Guid.NewGuid(),
            null, 
            null,
           "Others",
            "Others",
            "http://example.com/image.jpg"
        );
        
        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => 
        {
            await handler.Handle(new PostItemRequest(dto), CancellationToken.None);
        });
    }

    [Fact]
    public async Task Given_PostItemRequest_With_NullImageUrl_When_Handle_Then_ThrowsDbUpdateException()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext("663fff3c-00b6-41fa-9d8d-1887796af8a3");
        Mock<IMapper> mapperMock = CreateMapperMock();
        var handler = new PostItemHandler(dbContext, mapperMock.Object);
        var dto = new PostItemDto
        (
            Guid.NewGuid(),
            "Test Item",
            "This is a test item.",
            "Electronics",
            "New",
            null
        );


        // Act
        var result = await handler.Handle(new PostItemRequest(dto), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var createItem = await dbContext.Items.FirstOrDefaultAsync( i => i.Name == "Test Item");
        Assert.NotNull(createItem);
        Assert.Equal("This is a test item.", createItem.Description);
        Assert.Equal(ItemCategory.Electronics, createItem.Category);
        Assert.Equal(ItemCondition.New, createItem.Condition);
        Assert.Null(createItem.ImageUrl);
    }
    
    //TO DO: TESTS FOR MISSING REQUIRED FIELDS THAT THROW EXCEPTIONS AND LENGTH CONSTRAINTS ON STRINGS

}