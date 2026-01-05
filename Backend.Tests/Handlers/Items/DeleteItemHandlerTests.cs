using Backend.Features.Items.Enums;
using Backend.Data;
using Backend.Features.Items;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Handlers.Items;

public class DeleteItemHandlerTests
{
    private static ApplicationContext CreateInMemoryDbContext(string guid )
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: guid)
            .Options;

        var context = new ApplicationContext(options);
        return context;
    }

    [Fact]
    public async Task Given_ItemExists_When_Handle_Then_RemovesItemAndReturnsNoContent()
    {
        // Arrange
        var context = CreateInMemoryDbContext("3c3712b6-c9c4-44f8-88a6-c61f66f7a54c");
        var handler = new DeleteItemHandler(context);
        var itemId = Guid.Parse("cb397a9b-ec7c-4bb4-b683-363f07dd94d6");
        var item = new Item { Id = itemId, OwnerId = Guid.Parse("78154ffd-dffa-47c7-9743-f522dee1ca1e"), Name = "ToDelete", Description = "D", Category = ItemCategory.Clothing, Condition = ItemCondition.Good };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var request = new DeleteItemRequest(itemId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var removed = await context.Items.FindAsync(itemId);
        Assert.Null(removed);
    }

    [Fact]
    public async Task Given_ItemDoesNotExist_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext("97612d06-7360-4a15-be6b-ea5561772f4f");
        var handler = new DeleteItemHandler(context);
        var request = new DeleteItemRequest(Guid.Parse("a1b2c3d4-e9f6-4789-8abc-def012345678"));

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
    
    [Fact]
    public async Task Given_ExceptionOccurs_When_Handle_Then_ReturnsInternalServerError()
    {
        // Arrange
        var context = CreateInMemoryDbContext("d3f5e8c4-3f4b-4c2a-9f7e-8b9e6f1c2d3e");
        var handler = new DeleteItemHandler(context);
        var request = new DeleteItemRequest(Guid.Parse("a9b2c3d4-e5f6-4789-8abc-def012345678"));

        // Simulate exception by disposing the context before handling
        await context.DisposeAsync();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}