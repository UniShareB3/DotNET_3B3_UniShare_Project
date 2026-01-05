using AutoMapper;
using Backend.Data;
using Backend.Features.Items;
using Backend.Features.Items.DTO;
using Backend.Features.Items.Enums;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Tests.Handlers.Items;

public class GetItemHandlerTests
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
    public async Task Given_ItemExists_When_Handle_Then_ReturnsOkWithItem()
    {
        // Arrange
        var context = CreateInMemoryDbContext("02776839-a33e-4bba-b001-0167bf09e1b3");
        var itemId = Guid.Parse("cb397a9b-ec7c-4bb4-b683-363f07dd94d6");
        var item = new Item
        {
            Id = itemId, OwnerId = Guid.Parse("78154ffd-dffa-47c7-9743-f522dee1ca1e"), Name = "Item 1",
            Description = "D1", Category = ItemCategory.Clothing, Condition = ItemCondition.New
        };
        var user = new User { Id = item.OwnerId, FirstName = "John", LastName = "Doe", Email = "johndoe@gmail.com" };

        context.Users.Add(user);
        context.Items.Add(item);

        await context.SaveChangesAsync();
        var mapper = CreateMapper();

        var handler = new GetItemHandler(context, mapper);
        var request = new GetItemRequest(itemId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

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
        var context = CreateInMemoryDbContext("02776839-a33e-4bba-b001-0167bf09e1b3");
        var mapper = CreateMapper();
        
        var handler = new GetItemHandler(context, mapper);
        var request = new GetItemRequest(Guid.Parse("cb8f7efd-c4ad-4fd9-b100-0aa7fb0b3cc1"));

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
    
    [Fact]
    public async Task Given_ExceptionOccurs_When_Handle_Then_ReturnsProblem()
    {
        // Arrange
        var context = CreateInMemoryDbContext("02776839-a33e-4bba-b001-0167bf09e1b3");
        var mapper = CreateMapper();
        
        var handler = new GetItemHandler(context, mapper);
        var request = new GetItemRequest(Guid.Parse("cb8f7efd-c4ad-4fd9-b100-0aa7fb0b3cc1"));

        // Simulate exception by disposing the context before handling
        await context.DisposeAsync();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}