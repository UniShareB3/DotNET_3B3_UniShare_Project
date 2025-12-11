using Backend.Features.Items.DTO;
using Backend.Validators;
using FluentValidation.TestHelper;

namespace Backend.Tests.Validators;

public class PostItemDtoValidatorTests
{
    private readonly PostItemDtoValidator _validator = new();
    
    [Fact]
    public async Task Given_ValidPostItemDto_When_Validate_Then_NoErrors()
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
        
        // Act
        var result = await _validator.TestValidateAsync(dto);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public async Task Given_ValidPostItemDtoWithNullImageUrl_When_Validate_Then_NoErrors()
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
        
        // Act
        var result = await _validator.TestValidateAsync(dto);
        
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
        
        // Act
        var result = await _validator.TestValidateAsync(dto);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OwnerId);
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
        
        // Act
        var result = await _validator.TestValidateAsync(dto);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
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
        
        // Act
        var result = await _validator.TestValidateAsync(dto);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
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
        
        // Act
        var result = await _validator.TestValidateAsync(dto);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
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
        
        // Act
        var result = await _validator.TestValidateAsync(dto);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Category);
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
        
        // Act
        var result = await _validator.TestValidateAsync(dto);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Condition);
    }
    
    [Fact]
    public async Task Given_AllFieldsInvalid_When_Validate_Then_ReturnsMultipleValidationErrors()
    {
        // Arrange
        var dto = new PostItemDto(
            OwnerId: Guid.Empty,
            Name: "",
            Description: "",
            Category: "InvalidCategory",
            Condition: "InvalidCondition",
            ImageUrl: null
        );
        
        // Act
        var result = await _validator.TestValidateAsync(dto);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OwnerId);
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Description);
        result.ShouldHaveValidationErrorFor(x => x.Category);
        result.ShouldHaveValidationErrorFor(x => x.Condition);
    }
}