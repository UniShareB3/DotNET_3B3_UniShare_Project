using AutoMapper;
using Backend.Data;
using Backend.Features.Items.DTO;
using Backend.Features.Items.Enums;
using Backend.Features.Items.PatchItem;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Backend.Tests.Handlers.Items;

public class PatchItemHandlerTests
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
        var mapperMock = new Mock<IMapper>();
        mapperMock
            .Setup(m => m.Map<ItemDto>(It.IsAny<Item>()))
            .Returns((Item src) => new ItemDto
            {
                Id = src.Id,
                Name = src.Name,
                Description = src.Description,
                Category = src.Category.ToString(),
                Condition = src.Condition.ToString(),
                IsAvailable = src.IsAvailable,
                ImageUrl = src.ImageUrl,
                OwnerId = src.OwnerId,
                OwnerName = src.Owner?.UserName ?? "Unknown"
            });

        return mapperMock.Object;
    }

    [Fact]
    public async Task Given_NonExistentItem_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();
        var handler = new PatchItemHandler(context, mapper);

        var nonExistentItemId = Guid.NewGuid();
        var patchDto = new PatchItemDto(
            ItemName: "Updated Name",
            Description: null,
            Category: null,
            Condition: null,
            BlobName: null
        );

        var request = new PatchItemRequest(nonExistentItemId, patchDto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var notFoundResult = result as IStatusCodeHttpResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Given_ValidItemNameUpdate_When_Handle_Then_UpdatesItemName()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();
        var handler = new PatchItemHandler(context, mapper);

        var ownerId = Guid.NewGuid();
        var owner = new User
        {
            Id = ownerId,
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UniversityId = Guid.NewGuid()
        };

        var itemId = Guid.NewGuid();
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Original Name",
            Description = "Original Description",
            Category = ItemCategory.Books,
            Condition = ItemCondition.Good,
            Owner = owner
        };

        context.Users.Add(owner);
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var patchDto = new PatchItemDto(
            ItemName: "Updated Name",
            Description: null,
            Category: null,
            Condition: null,
            BlobName: null
        );

        var request = new PatchItemRequest(itemId, patchDto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Ok<ItemDto>>();
        var updatedItem = await context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
        updatedItem.Should().NotBeNull();
        updatedItem.Name.Should().Be("Updated Name");
        updatedItem.Description.Should().Be("Original Description"); // Should remain unchanged
    }

    [Fact]
    public async Task Given_ValidDescriptionUpdate_When_Handle_Then_UpdatesDescription()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();
        var handler = new PatchItemHandler(context, mapper);

        var ownerId = Guid.NewGuid();
        var owner = new User
        {
            Id = ownerId,
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UniversityId = Guid.NewGuid()
        };

        var itemId = Guid.NewGuid();
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Original Name",
            Description = "Original Description",
            Category = ItemCategory.Books,
            Condition = ItemCondition.Good,
            Owner = owner
        };

        context.Users.Add(owner);
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var patchDto = new PatchItemDto(
            ItemName: null,
            Description: "Updated Description",
            Category: null,
            Condition: null,
            BlobName: null
        );

        var request = new PatchItemRequest(itemId, patchDto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Ok<ItemDto>>();
        var updatedItem = await context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
        updatedItem.Should().NotBeNull();
        updatedItem.Description.Should().Be("Updated Description");
        updatedItem.Name.Should().Be("Original Name"); // Should remain unchanged
    }

    [Fact]
    public async Task Given_ValidCategoryUpdate_When_Handle_Then_UpdatesCategory()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();
        var handler = new PatchItemHandler(context, mapper);

        var ownerId = Guid.NewGuid();
        var owner = new User
        {
            Id = ownerId,
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UniversityId = Guid.NewGuid()
        };

        var itemId = Guid.NewGuid();
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Test Item",
            Description = "Test Description",
            Category = ItemCategory.Books,
            Condition = ItemCondition.Good,
            Owner = owner
        };

        context.Users.Add(owner);
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var patchDto = new PatchItemDto(
            ItemName: null,
            Description: null,
            Category: "Electronics",
            Condition: null,
            BlobName: null
        );

        var request = new PatchItemRequest(itemId, patchDto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Ok<ItemDto>>();
        var updatedItem = await context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
        updatedItem.Should().NotBeNull();
        updatedItem.Category.Should().Be(ItemCategory.Electronics);
    }

    [Fact]
    public async Task Given_InvalidCategory_When_Handle_Then_ReturnsBadRequest()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();
        var handler = new PatchItemHandler(context, mapper);

        var ownerId = Guid.NewGuid();
        var owner = new User
        {
            Id = ownerId,
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UniversityId = Guid.NewGuid()
        };

        var itemId = Guid.NewGuid();
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Test Item",
            Description = "Test Description",
            Category = ItemCategory.Books,
            Condition = ItemCondition.Good,
            Owner = owner
        };

        context.Users.Add(owner);
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var patchDto = new PatchItemDto(
            ItemName: null,
            Description: null,
            Category: "InvalidCategory",
            Condition: null,
            BlobName: null
        );

        var request = new PatchItemRequest(itemId, patchDto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var badRequestResult = result as IStatusCodeHttpResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        
        var updatedItem = await context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
        updatedItem!.Category.Should().Be(ItemCategory.Books); // Should remain unchanged
    }

    [Fact]
    public async Task Given_ValidConditionUpdate_When_Handle_Then_UpdatesCondition()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();
        var handler = new PatchItemHandler(context, mapper);

        var ownerId = Guid.NewGuid();
        var owner = new User
        {
            Id = ownerId,
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UniversityId = Guid.NewGuid()
        };

        var itemId = Guid.NewGuid();
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Test Item",
            Description = "Test Description",
            Category = ItemCategory.Books,
            Condition = ItemCondition.Good,
            Owner = owner
        };

        context.Users.Add(owner);
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var patchDto = new PatchItemDto(
            ItemName: null,
            Description: null,
            Category: null,
            Condition: "Excellent",
            BlobName: null
        );

        var request = new PatchItemRequest(itemId, patchDto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Ok<ItemDto>>();
        var updatedItem = await context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
        updatedItem.Should().NotBeNull();
        updatedItem.Condition.Should().Be(ItemCondition.Excellent);
    }

    [Fact]
    public async Task Given_InvalidCondition_When_Handle_Then_ReturnsBadRequest()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();
        var handler = new PatchItemHandler(context, mapper);

        var ownerId = Guid.NewGuid();
        var owner = new User
        {
            Id = ownerId,
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UniversityId = Guid.NewGuid()
        };

        var itemId = Guid.NewGuid();
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Test Item",
            Description = "Test Description",
            Category = ItemCategory.Books,
            Condition = ItemCondition.Good,
            Owner = owner
        };

        context.Users.Add(owner);
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var patchDto = new PatchItemDto(
            ItemName: null,
            Description: null,
            Category: null,
            Condition: "InvalidCondition",
            BlobName: null
        );

        var request = new PatchItemRequest(itemId, patchDto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var badRequestResult = result as IStatusCodeHttpResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        
        var updatedItem = await context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
        updatedItem!.Condition.Should().Be(ItemCondition.Good); // Should remain unchanged
    }

    [Fact]
    public async Task Given_ValidBlobNameUpdate_When_Handle_Then_UpdatesBlobName()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();
        var handler = new PatchItemHandler(context, mapper);

        var ownerId = Guid.NewGuid();
        var owner = new User
        {
            Id = ownerId,
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UniversityId = Guid.NewGuid()
        };

        var itemId = Guid.NewGuid();
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Test Item",
            Description = "Test Description",
            Category = ItemCategory.Books,
            Condition = ItemCondition.Good,
            BlobName = "original-blob.jpg",
            Owner = owner
        };

        context.Users.Add(owner);
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var patchDto = new PatchItemDto(
            ItemName: null,
            Description: null,
            Category: null,
            Condition: null,
            BlobName: "updated-blob.jpg"
        );

        var request = new PatchItemRequest(itemId, patchDto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Ok<ItemDto>>();
        var updatedItem = await context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
        updatedItem.Should().NotBeNull();
        updatedItem.BlobName.Should().Be("updated-blob.jpg");
    }

    [Fact]
    public async Task Given_NullBlobName_When_Handle_Then_DoesNotUpdateBlobName()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();
        var handler = new PatchItemHandler(context, mapper);

        var ownerId = Guid.NewGuid();
        var owner = new User
        {
            Id = ownerId,
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UniversityId = Guid.NewGuid()
        };

        var itemId = Guid.NewGuid();
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Test Item",
            Description = "Test Description",
            Category = ItemCategory.Books,
            Condition = ItemCondition.Good,
            BlobName = "existing-blob.jpg",
            Owner = owner
        };

        context.Users.Add(owner);
        context.Items.Add(item);
        await context.SaveChangesAsync();

        // Create a PatchItemDto with BlobName explicitly set to null
        // Note: In PATCH semantics, null means "don't update this field"
        var patchDto = new PatchItemDto(
            ItemName: null,
            Description: null,
            Category: null,
            Condition: null,
            BlobName: null
        );

        var request = new PatchItemRequest(itemId, patchDto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Ok<ItemDto>>();
        var updatedItem = await context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
        updatedItem.Should().NotBeNull();
        updatedItem.BlobName.Should().Be("existing-blob.jpg"); // Should remain unchanged
    }

    [Fact]
    public async Task Given_MultipleFieldsUpdate_When_Handle_Then_UpdatesAllFields()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();
        var handler = new PatchItemHandler(context, mapper);

        var ownerId = Guid.NewGuid();
        var owner = new User
        {
            Id = ownerId,
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UniversityId = Guid.NewGuid()
        };

        var itemId = Guid.NewGuid();
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Original Name",
            Description = "Original Description",
            Category = ItemCategory.Books,
            Condition = ItemCondition.Good,
            BlobName = "original-blob.jpg",
            Owner = owner
        };

        context.Users.Add(owner);
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var patchDto = new PatchItemDto(
            ItemName: "Updated Name",
            Description: "Updated Description",
            Category: "Electronics",
            Condition: "Excellent",
            BlobName: "updated-blob.jpg"
        );

        var request = new PatchItemRequest(itemId, patchDto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Ok<ItemDto>>();
        var updatedItem = await context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
        updatedItem.Should().NotBeNull();
        updatedItem.Name.Should().Be("Updated Name");
        updatedItem.Description.Should().Be("Updated Description");
        updatedItem.Category.Should().Be(ItemCategory.Electronics);
        updatedItem.Condition.Should().Be(ItemCondition.Excellent);
        updatedItem.BlobName.Should().Be("updated-blob.jpg");
    }

    [Fact]
    public async Task Given_EmptyStringsForOptionalFields_When_Handle_Then_DoesNotUpdateThoseFields()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();
        var handler = new PatchItemHandler(context, mapper);

        var ownerId = Guid.NewGuid();
        var owner = new User
        {
            Id = ownerId,
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UniversityId = Guid.NewGuid()
        };

        var itemId = Guid.NewGuid();
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Original Name",
            Description = "Original Description",
            Category = ItemCategory.Books,
            Condition = ItemCondition.Good,
            Owner = owner
        };

        context.Users.Add(owner);
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var patchDto = new PatchItemDto(
            ItemName: "",
            Description: "   ",
            Category: "",
            Condition: "  ",
            BlobName: null
        );

        var request = new PatchItemRequest(itemId, patchDto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Ok<ItemDto>>();
        var updatedItem = await context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
        updatedItem.Should().NotBeNull();
        updatedItem.Name.Should().Be("Original Name"); // Should remain unchanged
        updatedItem.Description.Should().Be("Original Description"); // Should remain unchanged
        updatedItem.Category.Should().Be(ItemCategory.Books); // Should remain unchanged
        updatedItem.Condition.Should().Be(ItemCondition.Good); // Should remain unchanged
    }

    [Fact]
    public async Task Given_CaseInsensitiveCategoryUpdate_When_Handle_Then_UpdatesCategory()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();
        var handler = new PatchItemHandler(context, mapper);

        var ownerId = Guid.NewGuid();
        var owner = new User
        {
            Id = ownerId,
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UniversityId = Guid.NewGuid()
        };

        var itemId = Guid.NewGuid();
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Test Item",
            Description = "Test Description",
            Category = ItemCategory.Books,
            Condition = ItemCondition.Good,
            Owner = owner
        };

        context.Users.Add(owner);
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var patchDto = new PatchItemDto(
            ItemName: null,
            Description: null,
            Category: "electronics",
            Condition: null,
            BlobName: null
        );

        var request = new PatchItemRequest(itemId, patchDto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Ok<ItemDto>>();
        var updatedItem = await context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
        updatedItem.Should().NotBeNull();
        updatedItem.Category.Should().Be(ItemCategory.Electronics);
    }

    [Fact]
    public async Task Given_CaseInsensitiveConditionUpdate_When_Handle_Then_UpdatesCondition()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();
        var handler = new PatchItemHandler(context, mapper);

        var ownerId = Guid.NewGuid();
        var owner = new User
        {
            Id = ownerId,
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UniversityId = Guid.NewGuid()
        };

        var itemId = Guid.NewGuid();
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Test Item",
            Description = "Test Description",
            Category = ItemCategory.Books,
            Condition = ItemCondition.Good,
            Owner = owner
        };

        context.Users.Add(owner);
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var patchDto = new PatchItemDto(
            ItemName: null,
            Description: null,
            Category: null,
            Condition: "excellent",
            BlobName: null
        );

        var request = new PatchItemRequest(itemId, patchDto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Ok<ItemDto>>();
        var updatedItem = await context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
        updatedItem.Should().NotBeNull();
        updatedItem.Condition.Should().Be(ItemCondition.Excellent);
    }

    [Fact]
    public async Task Given_ValidUpdate_When_Handle_Then_ReturnsMappedItemDto()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();
        var handler = new PatchItemHandler(context, mapper);

        var ownerId = Guid.NewGuid();
        var owner = new User
        {
            Id = ownerId,
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UniversityId = Guid.NewGuid()
        };

        var itemId = Guid.NewGuid();
        var item = new Item
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "Original Name",
            Description = "Original Description",
            Category = ItemCategory.Books,
            Condition = ItemCondition.Good,
            Owner = owner
        };

        context.Users.Add(owner);
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var patchDto = new PatchItemDto(
            ItemName: "Updated Name",
            Description: null,
            Category: null,
            Condition: null,
            BlobName: null
        );

        var request = new PatchItemRequest(itemId, patchDto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Ok<ItemDto>>();
        var okResult = result as Ok<ItemDto>;
        okResult.Should().NotBeNull();
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Id.Should().Be(itemId);
        okResult.Value.Name.Should().Be("Updated Name");
        okResult.Value.OwnerId.Should().Be(ownerId);
        okResult.Value.OwnerName.Should().Be("testuser");
    }

    [Fact]
    public async Task Given_AllCategoryValues_When_Handle_Then_UpdatesCorrectly()
    {
        // Test all enum values for Category
        var categories = new[] { "Others", "Books", "Electronics", "Kitchen", "Clothing", "Accessories" };
        var expectedEnums = new[] { ItemCategory.Others, ItemCategory.Books, ItemCategory.Electronics, 
            ItemCategory.Kitchen, ItemCategory.Clothing, ItemCategory.Accessories };

        for (int i = 0; i < categories.Length; i++)
        {
            // Arrange
            var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var mapper = CreateMapper();
            var handler = new PatchItemHandler(context, mapper);

            var ownerId = Guid.NewGuid();
            var owner = new User
            {
                Id = ownerId,
                UserName = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UniversityId = Guid.NewGuid()
            };

            var itemId = Guid.NewGuid();
            var item = new Item
            {
                Id = itemId,
                OwnerId = ownerId,
                Name = "Test Item",
                Description = "Test Description",
                Category = ItemCategory.Books,
                Condition = ItemCondition.Good,
                Owner = owner
            };

            context.Users.Add(owner);
            context.Items.Add(item);
            await context.SaveChangesAsync();

            var patchDto = new PatchItemDto(
                ItemName: null,
                Description: null,
                Category: categories[i],
                Condition: null,
                BlobName: null
            );

            var request = new PatchItemRequest(itemId, patchDto);

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<Ok<ItemDto>>();
            var updatedItem = await context.Items.FirstOrDefaultAsync(it => it.Id == itemId);
            updatedItem!.Category.Should().Be(expectedEnums[i]);
        }
    }

    [Fact]
    public async Task Given_AllConditionValues_When_Handle_Then_UpdatesCorrectly()
    {
        // Test all enum values for Condition
        var conditions = new[] { "New", "Excellent", "Good", "Fair", "Poor" };
        var expectedEnums = new[] { ItemCondition.New, ItemCondition.Excellent, ItemCondition.Good, 
            ItemCondition.Fair, ItemCondition.Poor };

        for (int i = 0; i < conditions.Length; i++)
        {
            // Arrange
            var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var mapper = CreateMapper();
            var handler = new PatchItemHandler(context, mapper);

            var ownerId = Guid.NewGuid();
            var owner = new User
            {
                Id = ownerId,
                UserName = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UniversityId = Guid.NewGuid()
            };

            var itemId = Guid.NewGuid();
            var item = new Item
            {
                Id = itemId,
                OwnerId = ownerId,
                Name = "Test Item",
                Description = "Test Description",
                Category = ItemCategory.Books,
                Condition = ItemCondition.Good,
                Owner = owner
            };

            context.Users.Add(owner);
            context.Items.Add(item);
            await context.SaveChangesAsync();

            var patchDto = new PatchItemDto(
                ItemName: null,
                Description: null,
                Category: null,
                Condition: conditions[i],
                BlobName: null
            );

            var request = new PatchItemRequest(itemId, patchDto);

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<Ok<ItemDto>>();
            var updatedItem = await context.Items.FirstOrDefaultAsync(it => it.Id == itemId);
            updatedItem!.Condition.Should().Be(expectedEnums[i]);
        }
    }
}

