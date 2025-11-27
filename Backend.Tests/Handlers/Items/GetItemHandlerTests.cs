using Backend.Data;
using Backend.Features.Items;
using Backend.Features.Items.DTO;
using Backend.Features.Items.Enums;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Handlers.Items;

public class GetItemHandlerTests
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
    public async Task Given_ItemExists_When_Handle_Then_ReturnsOkWithItem()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext("02776839-a33e-4bba-b001-0167bf09e1b3");
        var itemId = Guid.Parse("cb397a9b-ec7c-4bb4-b683-363f07dd94d6");
        var item = new Item { Id = itemId, OwnerId = Guid.Parse("78154ffd-dffa-47c7-9743-f522dee1ca1e"), Name = "Item 1", Description = "D1", Category = ItemCategory.Clothing, Condition = ItemCondition.New };
        var user = new User { Id = item.OwnerId, FirstName = "John", LastName = "Doe", Email = "johndoe@gmail.com" };
        
        dbContext.Users.Add(user);
        dbContext.Items.Add(item);
        
        await dbContext.SaveChangesAsync();

        var handler = new GetItemHandler(dbContext);
        var request = new GetItemRequest(itemId);

        // Act
        var result = await handler.Handle(request,  CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        var returned = valueResult.Value.Should().BeAssignableTo<ItemDto>().Subject;

        returned.Id.Should().Be(itemId);
        returned.Name.Should().Be("Item 1");
    }

    [Fact]
    public async Task Given_ItemDoesNotExist_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext("02776839-a33e-4bba-b001-0167bf09e1b3");
        var handler = new GetItemHandler(dbContext);
        var request = new GetItemRequest(Guid.Parse("cb8f7efd-c4ad-4fd9-b100-0aa7fb0b3cc1"));

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}