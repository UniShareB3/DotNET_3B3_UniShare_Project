using Backend.Features.Items.DTO;
using Backend.Features.Items.PostItem;
using Backend.Validators;
using FluentValidation.TestHelper;

namespace Backend.Tests.Validators;

public class PostItemRequestValidatorTests
{
    private readonly PostItemRequestValidator _validator;
    
    public PostItemRequestValidatorTests()
    {
        var itemDtoValidator = new PostItemDtoValidator();
        _validator = new PostItemRequestValidator(itemDtoValidator);
    }
    
    [Fact]
    public async Task Given_ValidPostItemRequest_When_Validate_Then_NoErrors()
    {
        // Arrange
        var dto = new PostItemDto(
            OwnerId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name: "Test Item",
            Description: "A valid test item description",
            Category: "Electronics",
            Condition: "New",
            ImageUrl: "https://example.com/image.jpg"
        );
        var request = new PostItemRequest(dto);
        
        // Act
        var result = await _validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public async Task Given_ValidPostItemRequestWithNullImageUrl_When_Validate_Then_NoErrors()
    {
        // Arrange
        var dto = new PostItemDto(
            OwnerId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name: "Test Item",
            Description: "A valid test item description",
            Category: "Books",
            Condition: "Excellent",
            ImageUrl: null
        );
        var request = new PostItemRequest(dto);
        
        // Act
        var result = await _validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public async Task Given_EmptyOwnerId_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new PostItemDto(
            OwnerId: Guid.Empty,
            Name: "Test Item",
            Description: "A valid test item description",
            Category: "Electronics",
            Condition: "New",
            ImageUrl: null
        );
        var request = new PostItemRequest(dto);
        
        // Act
        var result = await _validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Item.OwnerId);
    }
    
    [Fact]
    public async Task Given_EmptyName_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new PostItemDto(
            OwnerId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Name: "",
            Description: "A valid test item description",
            Category: "Electronics",
            Condition: "New",
            ImageUrl: null
        );
        var request = new PostItemRequest(dto);
        
        // Act
        var result = await _validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Item.Name);
    }
    
    [Fact]
    public async Task Given_NullName_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new PostItemDto(
            OwnerId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Name: null!,
            Description: "A valid test item description",
            Category: "Electronics",
            Condition: "New",
            ImageUrl: null
        );
        var request = new PostItemRequest(dto);
        
        // Act
        var result = await _validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Item.Name);
    }
    
    [Fact]
    public async Task Given_NameExceeds100Characters_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var longName = new string('A', 101);
        var dto = new PostItemDto(
            OwnerId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Name: longName,
            Description: "A valid test item description",
            Category: "Electronics",
            Condition: "New",
            ImageUrl: null
        );
        var request = new PostItemRequest(dto);
        
        // Act
        var result = await _validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Item.Name);
    }
    
    [Fact]
    public async Task Given_NameExactly100Characters_When_Validate_Then_NoErrors()
    {
        // Arrange
        var exactName = new string('A', 100);
        var dto = new PostItemDto(
            OwnerId: Guid.Parse("66666666-6666-6666-6666-666666666666"),
            Name: exactName,
            Description: "A valid test item description",
            Category: "Electronics",
            Condition: "New",
            ImageUrl: null
        );
        var request = new PostItemRequest(dto);
        
        // Act
        var result = await _validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Item.Name);
    }
    
    [Fact]
    public async Task Given_EmptyDescription_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new PostItemDto(
            OwnerId: Guid.Parse("77777777-7777-7777-7777-777777777777"),
            Name: "Test Item",
            Description: "",
            Category: "Electronics",
            Condition: "New",
            ImageUrl: null
        );
        var request = new PostItemRequest(dto);
        
        // Act
        var result = await _validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Item.Description);
    }
    
    [Fact]
    public async Task Given_DescriptionExceeds1000Characters_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var longDescription = new string('A', 1001);
        var dto = new PostItemDto(
            OwnerId: Guid.Parse("99999999-9999-9999-9999-999999999999"),
            Name: "Test Item",
            Description: longDescription,
            Category: "Electronics",
            Condition: "New",
            ImageUrl: null
        );
        var request = new PostItemRequest(dto);
        
        // Act
        var result = await _validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Item.Description);
    }
    
    [Fact]
    public async Task Given_EmptyCategory_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new PostItemDto(
            OwnerId: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Name: "Test Item",
            Description: "A valid test item description",
            Category: "",
            Condition: "New",
            ImageUrl: null
        );
        var request = new PostItemRequest(dto);
        
        // Act
        var result = await _validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Item.Category);
    }
    
    [Fact]
    public async Task Given_InvalidCategory_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new PostItemDto(
            OwnerId: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Name: "Test Item",
            Description: "A valid test item description",
            Category: "InvalidCategory",
            Condition: "New",
            ImageUrl: null
        );
        var request = new PostItemRequest(dto);
        
        // Act
        var result = await _validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Item.Category);
    }
    
    [Fact]
    public async Task Given_EmptyCondition_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new PostItemDto(
            OwnerId: Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            Name: "Test Item",
            Description: "A valid test item description",
            Category: "Electronics",
            Condition: "",
            ImageUrl: null
        );
        var request = new PostItemRequest(dto);
        
        // Act
        var result = await _validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Item.Condition);
    }
    
    [Fact]
    public async Task Given_InvalidCondition_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new PostItemDto(
            OwnerId: Guid.Parse("11112222-3333-4444-5555-666677778888"),
            Name: "Test Item",
            Description: "A valid test item description",
            Category: "Electronics",
            Condition: "InvalidCondition",
            ImageUrl: null
        );
        var request = new PostItemRequest(dto);
        
        // Act
        var result = await _validator.TestValidateAsync(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Item.Condition);
    }
    
    
}