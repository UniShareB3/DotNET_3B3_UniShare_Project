using Backend.Data;
using Backend.Features.Items;
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
        var handler = new GetItemsHandler(dbContext);
        
        var userId = Guid.Parse("cb397a9b-ec7c-4bb4-b683-363f07dd94d6");
        dbContext.Items.AddRange(
            new Item { OwnerId = userId, Name = "Item 1", Description = "D1", Category = "C1", Condition = "New" },
            new Item { OwnerId = userId, Name = "Item 2", Description = "D2", Category = "C2", Condition = "Used" }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var result = await handler.Handle();

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        
        var items = valueResult.Value.Should().BeAssignableTo<List<Item>>().Subject;
        
        items.Should().HaveCount(2);
        items.Should().Contain(i => i.Name == "Item 1");
        items.Should().Contain(i => i.Name == "Item 2");
    }

    [Fact]
    public async Task Given_NoItemsExist_When_Handle_Then_ReturnsOkWithEmptyList()
    {
        //Arrange
        var dbContext = CreateInMemoryDbContext("0a1dc121-52db-4baf-be9e-e88d2d93d4c5"); 
        var handler = new GetItemsHandler(dbContext);
        
        //Act
        var result = await handler.Handle();
        
        //Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        var items = valueResult.Value.Should().BeAssignableTo<List<Item>>().Subject;
        
        items.Should().HaveCount(0);
    }

}