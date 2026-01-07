using Backend.Features.ModeratorAssignment.DTO;
using Backend.Features.ModeratorAssignment.Enums;
using Backend.Validators;
using FluentAssertions;

namespace Backend.Tests.Validators;

public class UpdateModeratorAssignmentStatusValidatorTests
{
    [Fact]
    public async Task Given_ValidRequest_When_StatusIsPending_Then_ReturnsValid()
    {
        // Arrange
        Guid adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dto = new UpdateModeratorAssignmentStatusDto(ModeratorAssignmentStatus.Pending, adminId);
        var validator = new UpdateModeratorAssignmentStatusDtoValidator();
        
        // Act
        var result = await validator.ValidateAsync(dto);
        
        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_ValidRequest_When_StatusIsAccepted_Then_ReturnsValid()
    {
        // Arrange
        Guid adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dto = new UpdateModeratorAssignmentStatusDto(ModeratorAssignmentStatus.Accepted, adminId);
        var validator = new UpdateModeratorAssignmentStatusDtoValidator();
        
        // Act
        var result = await validator.ValidateAsync(dto);
        
        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_ValidRequest_When_StatusIsRejected_Then_ReturnsValid()
    {
        // Arrange
        Guid adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dto = new UpdateModeratorAssignmentStatusDto(ModeratorAssignmentStatus.Rejected, adminId);
        var validator = new UpdateModeratorAssignmentStatusDtoValidator();
        
        // Act
        var result = await validator.ValidateAsync(dto);
        
        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_Request_When_ReviewedByAdminIdIsEmpty_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new UpdateModeratorAssignmentStatusDto(ModeratorAssignmentStatus.Accepted, Guid.Empty);
        var validator = new UpdateModeratorAssignmentStatusDtoValidator();
        
        // Act
        var result = await validator.ValidateAsync(dto);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Reviewer Admin ID is required");
    }

    [Fact]
    public async Task Given_Request_When_StatusIsInvalidEnum_Then_ReturnsValidationError()
    {
        // Arrange
        Guid adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        // Cast an invalid integer to the enum to simulate invalid enum value
        var dto = new UpdateModeratorAssignmentStatusDto((ModeratorAssignmentStatus)999, adminId);
        var validator = new UpdateModeratorAssignmentStatusDtoValidator();
        
        // Act
        var result = await validator.ValidateAsync(dto);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Status must be a valid ModeratorAssignmentStatus value");
    }

    [Fact]
    public async Task Given_Request_When_BothFieldsAreInvalid_Then_ReturnsMultipleValidationErrors()
    {
        // Arrange
        var dto = new UpdateModeratorAssignmentStatusDto((ModeratorAssignmentStatus)999, Guid.Empty);
        var validator = new UpdateModeratorAssignmentStatusDtoValidator();
        
        // Act
        var result = await validator.ValidateAsync(dto);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.ErrorMessage == "Status must be a valid ModeratorAssignmentStatus value");
        result.Errors.Should().Contain(e => e.ErrorMessage == "Reviewer Admin ID is required");
    }

    [Theory]
    [InlineData(ModeratorAssignmentStatus.Pending)]
    [InlineData(ModeratorAssignmentStatus.Accepted)]
    [InlineData(ModeratorAssignmentStatus.Rejected)]
    public async Task Given_ValidRequest_When_AllValidStatusValues_Then_ReturnsValid(ModeratorAssignmentStatus status)
    {
        // Arrange
        Guid adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dto = new UpdateModeratorAssignmentStatusDto(status, adminId);
        var validator = new UpdateModeratorAssignmentStatusDtoValidator();
        
        // Act
        var result = await validator.ValidateAsync(dto);
        
        // Assert
        result.IsValid.Should().BeTrue();
    }
}

