﻿using Backend.Features.Items.Enums;

namespace Backend.Tests.Handlers.Items;

using Backend.Data;
using Backend.Features.Items;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class DeleteItemHandlerTests
{
    private static ApplicationContext CreateInMemoryDbContext(string guid )
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: guid)
            .Options;

        var dbContext = new ApplicationContext(options);
        return dbContext;
    }

    [Fact]
    public async Task Given_ItemExists_When_Handle_Then_RemovesItemAndReturnsNoContent()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext("3c3712b6-c9c4-44f8-88a6-c61f66f7a54c");
        var handler = new DeleteItemHandler(dbContext);
        var itemId = Guid.Parse("cb397a9b-ec7c-4bb4-b683-363f07dd94d6");
        var item = new Item { Id = itemId, OwnerId = Guid.Parse("78154ffd-dffa-47c7-9743-f522dee1ca1e"), Name = "ToDelete", Description = "D", Category = ItemCategory.Clothing, Condition = ItemCondition.Good };
        dbContext.Items.Add(item);
        await dbContext.SaveChangesAsync();

        var request = new DeleteItemRequest(itemId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status204NoContent);

        var removed = await dbContext.Items.FindAsync(itemId);
        Assert.Null(removed);
    }

    [Fact]
    public async Task Given_ItemDoesNotExist_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext("97612d06-7360-4a15-be6b-ea5561772f4f");
        var handler = new DeleteItemHandler(dbContext);
        var request = new DeleteItemRequest(Guid.NewGuid());

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}