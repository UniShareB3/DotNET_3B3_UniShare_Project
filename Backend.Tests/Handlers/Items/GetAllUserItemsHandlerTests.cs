using AutoMapper;
using Backend.Data;
using Backend.Features.Items;
using Backend.Features.Items.DTO;
using Backend.Features.Items.Enums;
using Backend.Features.Items.GetAllUserItems;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

    private static IMapper CreateMapper()
    {
        using var loggerFactory = new LoggerFactory();
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Item, ItemDto>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.ToString()))
                .ForMember(dest => dest.Condition, opt => opt.MapFrom(src => src.Condition.ToString()))
                .ForMember(dest => dest.OwnerName, opt => opt.MapFrom(src => (src.Owner.FirstName + " " + src.Owner.LastName).Trim()));
        }, loggerFactory);

        return config.CreateMapper();
    }

    [Fact]
    public async Task Given_UserWithItems_When_GettingAllItems_Then_ReturnsOnlyUsersItems()
    {
        // Arrange
        var context = CreateInMemoryDbContext("cb397a9b-ec7c-4bb4-b683-363f07dd94da");
        var mapper = CreateMapper();
        var userId = Guid.Parse("cb397a9b-ec7c-4bb4-b683-363f07dd94db");
        var otherUserId = Guid.Parse("cb397a9b-ec7c-4bb4-b683-363f07dd94da");

        var user = new User { Id = userId, FirstName = "Test", LastName = "User", Email = "test@test.com", UserName = "testuser" };
        var otherUser = new User { Id = otherUserId, FirstName = "Other", LastName = "User", Email = "other@test.com", UserName = "otheruser" };
        context.Users.AddRange(user, otherUser);

        var userItems = new List<Item>
        {
            new Item
            {
                Id = Guid.Parse("a112c3d4-e5f6-4789-8abc-def012345678"), OwnerId = userId, Owner = user, Name = "User Item 1", Description = "Desc 1",
                Category = ItemCategory.Electronics, Condition = ItemCondition.New
            },
            new Item
            {
                Id = Guid.Parse("11b2c3d4-e5f6-4789-8abc-def012345678"), OwnerId = userId, Owner = user, Name = "User Item 2", Description = "Desc 2",
                Category = ItemCategory.Books, Condition = ItemCondition.Good
            }
        };
        var otherUserItems = new List<Item>
        {
            new Item
            {
                Id = Guid.Parse("1112c3d4-e5f6-4789-8abc-def012345678"), OwnerId = otherUserId, Owner = otherUser, Name = "Other User Item 1", Description = "Desc 3",
                Category = ItemCategory.Clothing, Condition = ItemCondition.Fair
            }
        };
        context.Items.AddRange(userItems);
        context.Items.AddRange(otherUserItems);
        await context.SaveChangesAsync();

        var handler = new GetAllUserItemsHandler(context, mapper);

        // Act
        var result = await handler.Handle(new GetAllUserItemsRequest(userId), CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<List<ItemDto>>>().Subject;
        okResult.Value.Should().HaveCount(2);
        okResult.Value.Should().AllSatisfy(item => item.OwnerName.Should().Be("Test User"));
    }
    
    [Fact]
    public async Task Given_UserWithNoItems_When_GettingAllItems_Then_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateInMemoryDbContext("aa397a9b-ec7c-4bb4-b683-363f07dd94d6");
        var mapper = CreateMapper();
        var userId = Guid.Parse("bb397a9b-ec7c-4bb4-b683-363f07dd94d6");

        var handler = new GetAllUserItemsHandler(context, mapper);

        // Act
        var result = await handler.Handle(new GetAllUserItemsRequest(userId), CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<List<ItemDto>>>().Subject;
        okResult.Value.Should().BeEmpty();
    }
    
    [Fact]
    public async Task Given_UserWithInvalidId_When_GettingAllItems_Then_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateInMemoryDbContext("cb397a9b-ec7c-4bb4-b683-363f07dd9rrr4d6");
        var mapper = CreateMapper();
        var invalidUserId = Guid.Parse("cb397a9b-ec7c-4bb4-b683-363f07dd2222");

        var handler = new GetAllUserItemsHandler(context, mapper);

        // Act
        var result = await handler.Handle(new GetAllUserItemsRequest(invalidUserId), CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<List<ItemDto>>>().Subject;
        okResult.Value.Should().BeEmpty();
    }
    
    [Fact]
    public async Task Given_ExceptionOccurs_When_GettingAllItems_Then_HandlesException()
    {
        // Arrange
        var context = CreateInMemoryDbContext("cb397a9b-ec7c-4bb4-b683-363f07dd9excp");
        var userId = Guid.Parse("a1b2c3d4-15f6-4789-8abc-def012345678");
        
        // Add an item to trigger the mapper 
        var user = new User { Id = userId, FirstName = "Test", LastName = "User", Email = "test@test.com", UserName = "testuser" };
        context.Users.Add(user);
        context.Items.Add(new Item
        {
            Id = Guid.Parse("a1b2c3d4-15f6-4789-81bc-def012345678"), OwnerId = userId, Owner = user, Name = "Item", Description = "Desc",
            Category = ItemCategory.Electronics, Condition = ItemCondition.New
        });
        await context.SaveChangesAsync();
        
        var handler = new GetAllUserItemsHandler(context, null!);

        // Act
        var result = await handler.Handle(new GetAllUserItemsRequest(userId), CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}