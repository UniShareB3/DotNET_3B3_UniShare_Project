using Backend.Data;
using Backend.Features.Items.Enums;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Handlers.Items;

public class GetAllUserItemsHandlerTests
{
    private static ApplicationContext CreateInMemoryDbContext(string guid)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: guid)
            .Options;

        var context = new ApplicationContext(options);
        return context;
    }

    [Fact]
    public async Task Given_UserWithItems_When_GettingAllItems_Then_ReturnsOnlyUsersItems()
    {
        // Arrange
        var context = CreateInMemoryDbContext("cb397a9b-ec7c-4bb4-b683-363f07dd94da");
        var userId = Guid.Parse("cb397a9b-ec7c-4bb4-b683-363f07dd94db");
        var otherUserId = Guid.Parse("cb397a9b-ec7c-4bb4-b683-363f07dd94da");
        var userItems = new List<Item>
        {
            new Item
            {
                Id = Guid.NewGuid(), OwnerId = userId, Name = "User Item 1", Description = "Desc 1",
                Category = ItemCategory.Electronics, Condition = ItemCondition.New
            },
            new Item
            {
                Id = Guid.NewGuid(), OwnerId = userId, Name = "User Item 2", Description = "Desc 2",
                Category = ItemCategory.Books, Condition = ItemCondition.Good
            }
        };
        var otherUserItems = new List<Item>
        {
            new Item
            {
                Id = Guid.NewGuid(), OwnerId = otherUserId, Name = "Other User Item 1", Description = "Desc 3",
                Category = ItemCategory.Clothing, Condition = ItemCondition.Fair
            }
        };
        context.Items.AddRange(userItems);
        context.Items.AddRange(otherUserItems);
        await context.SaveChangesAsync();

        // Act
        var retrievedItems = await context.Items
            .Where(i => i.OwnerId == userId)
            .ToListAsync();

        // Assert
        Assert.Equal(2, retrievedItems.Count);
        Assert.All(retrievedItems, item => Assert.Equal(userId, item.OwnerId));
    }
    
    [Fact]
    public async Task Given_UserWithNoItems_When_GettingAllItems_Then_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateInMemoryDbContext("aa397a9b-ec7c-4bb4-b683-363f07dd94d6");
        var userId = Guid.Parse("bb397a9b-ec7c-4bb4-b683-363f07dd94d6");

        // Act
        var retrievedItems = await context.Items
            .Where(i => i.OwnerId == userId)
            .ToListAsync();

        // Assert
        Assert.Empty(retrievedItems);
    }
    
    [Fact]
    public async Task Given_UserWithInvalidId_When_GettingAllItems_Then_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateInMemoryDbContext("cb397a9b-ec7c-4bb4-b683-363f07dd9rrr4d6");
        var invalidUserId = Guid.Parse("cb397a9b-ec7c-4bb4-b683-363f07dd2222");

        // Act
        var retrievedItems = await context.Items
            .Where(i => i.OwnerId == invalidUserId)
            .ToListAsync();

        // Assert
        Assert.Empty(retrievedItems);
    }
}