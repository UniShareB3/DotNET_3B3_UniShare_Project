using AutoMapper;
using Backend.Data;
using Backend.Features.Items;
using Backend.Features.Items.DTO;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Tests.Handlers.Items;

public class GetUserItemHandlerTests
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
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Item, ItemDto>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.ToString()))
                .ForMember(dest => dest.Condition, opt => opt.MapFrom(src => src.Condition.ToString()));
        }, new LoggerFactory());

        return config.CreateMapper();
    }

    [Fact]
    public async Task Given_UserWithItems_When_Handle_Then_ReturnsUserItems()
    {
        // Arrange
        var context = CreateInMemoryDbContext("d290f1ee-0000-4b01-90e6-d701748f0851");
        var mapper = CreateMapper();
        var userId = Guid.Parse("d290f1ee-6c54-4b01-90e6-d701748f0851");
        
        var user = new User 
        { 
            Id = userId, 
            FirstName = "Test", 
            LastName = "User", 
            Email = "test@student.tuiasi.ro" 
        };
        context.Users.Add(user);
        var item1 = new Item 
        { 
            Id = Guid.Parse("d290f1ee-6c54-4b01-90e6-d701748f0000"), 
            OwnerId = userId, 
            Name = "Item 1", 
            Description = "Description 1", 
            Category = Features.Items.Enums.ItemCategory.Electronics, 
            Condition = Features.Items.Enums.ItemCondition.New 
        };
        var item2 = new Item 
        { 
            Id = Guid.Parse("d290f1ee-6c54-4b01-90e6-d701748f0001"), 
            OwnerId = userId, 
            Name = "Item 2", 
            Description = "Description 2", 
            Category = Features.Items.Enums.ItemCategory.Books, 
            Condition = Features.Items.Enums.ItemCondition.Excellent 
        };
        
        context.Items.AddRange(item1, item2);
        await context.SaveChangesAsync();
        
        var handler = new GetUserItemHandler(context, mapper);
        var request = new GetUserItemRequest(userId,item1.Id);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        var item = valueResult.Value.Should().BeAssignableTo<ItemDto>().Subject;
        
        item.Id.Should().Be(item1.Id);
        item.Name.Should().Be("Item 1");
        item.Description.Should().Be("Description 1");
        item.Category.Should().Be("Electronics");
        item.Condition.Should().Be("New");
    }

    [Fact]
    public async Task Given_UserWithoutItems_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext("d290f1ee-0000-4b01-90e6-d701748f0852");
        var mapper = CreateMapper();
        var userId = Guid.Parse("d290f1ee-6c54-4b01-90e6-d701748f0852");
        var handler = new GetUserItemHandler(context, mapper);
        var request = new GetUserItemRequest(userId, Guid.Parse("d290f1ee-6c54-4b01-90e6-d701748f0002"));
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Given_UserWithDifferentItem_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext("d290f1ee-0000-4b01-90e6-d701748f0853");
        var mapper = CreateMapper();
        var userId = Guid.Parse("d290f1ee-6c54-4b01-90e6-d701748f0853");

        var user = new User
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@studet.uaic.ro"
        };
        context.Users.Add(user);

        var item = new Item
        {
            Id = Guid.Parse("d290f1ee-6c54-4b01-90e6-d701748f0003"),
            OwnerId = Guid.Parse("d290f1ee-6c54-4b01-90e6-d701748f9999"), // Different owner
            Name = "Item 1",
            Description = "Description 1",
            Category = Features.Items.Enums.ItemCategory.Electronics,
            Condition = Features.Items.Enums.ItemCondition.New
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();
        var handler = new GetUserItemHandler(context, mapper);
        var request = new GetUserItemRequest(userId, item.Id);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}