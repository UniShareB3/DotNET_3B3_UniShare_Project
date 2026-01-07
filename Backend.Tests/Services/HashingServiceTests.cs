using Backend.Services.Hashing;
using FluentAssertions;

namespace Backend.Tests.Services;

public class HashingServiceTests
{
    [Fact]
    public void Given_String_When_HashCode_Then_ReturnsCorrectHash()
    {
        // Arrange
        var hashingService = new HashingService();
        var input = "UniShareTest";
        var expectedHash =
            "cuMoKF3ymsCfkYyFDTHHRFKg5FAZZiLuUoREAi/chnM=";
        
        // Act
        var actualHash = hashingService.HashCode(input);
        
        // Assert
        actualHash.Should().Be(expectedHash);
    }
}