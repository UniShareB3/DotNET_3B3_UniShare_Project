using Backend.Features.Universities.DTO;
using FluentAssertions;

namespace Backend.Tests.Handlers.Universities.DTO;

public class CreateUniversityDtoTests
{
    [Fact]
    public void Given_ValidParameters_When_CreateInstance_Then_PropertiesAreSetCorrectly()
    {
        // Arrange
        var name = "Test University";
        var shortCode = "TU";
        var EmailDomain = "testuniversity.edu";
        var id = Guid.Parse("12345678-1234-1234-1234-1234567890ab");

        // Act
        var dto = new UniversityDto
        {
            Id = id,
            Name = name,
            ShortCode = shortCode,
            EmailDomain = EmailDomain
        };

        // Assert
        dto.Name.Should().Be(name);
        dto.ShortCode.Should().Be(shortCode);
        dto.EmailDomain.Should().Be(EmailDomain);
        dto.Id.Should().Be(id);
    }
}

