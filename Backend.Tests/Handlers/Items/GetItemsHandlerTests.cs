﻿using Backend.Data;
using Backend.Features.Items;
using Backend.Features.Items.DTO;
using Backend.Features.Items.Enums;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Handlers.Items;

public class GetItemsHandlerTests
{
    private static ApplicationContext CreateInMemoryDbContext(string guid)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: guid)
            .Options;

        var dbContext = new ApplicationContext(options);
        return dbContext;
    }

    [Fact]
    public async Task Given_ItemsExist_When_Handle_Then_ReturnsOkWithAllItems()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext("163edd1c-3e7a-4f57-9dac-7e8d17c509f8"); 
        var handler = new GetAllItemsHandler(dbContext);
        
        var userId = Guid.Parse("cb397a9b-ec7c-4bb4-b683-363f07dd94d6");
        var user = new User { Id = userId, FirstName = "Test", LastName = "User" };
        dbContext.Users.Add(user);

        dbContext.Items.AddRange(
            new Item { OwnerId = userId, Name = "Item 1", Description = "D1", Category = ItemCategory.Clothing, Condition = ItemCondition.New, Owner = user },
            new Item { OwnerId = userId, Name = "Item 2", Description = "D2", Category = ItemCategory.Books, Condition = ItemCondition.Fair, Owner = user }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var result = await handler.Handle(new GetAllItemsRequest(), CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        
        var items = valueResult.Value.Should().BeAssignableTo<List<ItemDto>>().Subject;
        
        items.Should().HaveCount(2);
        items.Should().Contain(i => i.Name == "Item 1");
        items.Should().Contain(i => i.Name == "Item 2");
    }

    [Fact]
    public async Task Given_NoItemsExist_When_Handle_Then_ReturnsOkWithEmptyList()
    {
        //Arrange
        var dbContext = CreateInMemoryDbContext("0a1dc121-52db-4baf-be9e-e88d2d93d4c5"); 
        var handler = new GetAllItemsHandler(dbContext);
        
        //Act
        var result = await handler.Handle(new GetAllItemsRequest(), CancellationToken.None);
        
        //Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        var items = valueResult.Value.Should().BeAssignableTo<List<ItemDto>>().Subject;
        
        items.Should().HaveCount(0);
    }

}