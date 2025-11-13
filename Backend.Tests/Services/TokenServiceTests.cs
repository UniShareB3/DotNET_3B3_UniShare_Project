using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.TokenGenerators;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Tests.Services;

public class TokenServiceTests
{
    //create an in-memory IConfiguration with JwtSettings used by TokenService
    private static IConfiguration CrateInMemoryConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"JwtSettings:Key", "ThisIsASecretKeyForJwtTokenGeneration12345"},
            {"JwtSettings:Issuer", "MyAppIssuer"},
            {"JwtSettings:Audience", "MyAppAudience"},
            {"JwtSettings:ExpiryMinutes", "60"}
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public void Given_ValidUser_When_GenerateToken_Then_ReturnsValidJwtToken()
    {
        
        // Arrange
        var configuration = CrateInMemoryConfiguration();
        var tokenService = new TokenGenerators.TokenService(configuration);
        var user = new Data.User
        {
            Id = Guid.Parse("cb397a9b-ec7c-4bb4-b683-363f07dd94d6"),
            Email = "email@student.uaic.ro",
        };
        
        // Act
        var token = tokenService.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        
        // Assert
        Assert.NotNull(userIdClaim);
        Assert.Equal(user.Id.ToString(), userIdClaim!.Value);
        Assert.NotNull(emailClaim);
        Assert.Equal(user.Email, emailClaim!.Value);
    }
    
    [Fact]
    public void Given_TokenService_When_GenerateRefreshToken_Then_ReturnsValidBase64AndIsRandom()
    {
        // Arrange
        var configuration = CrateInMemoryConfiguration();
        var tokenService = new TokenService(configuration);

        // Act
        var token1 = tokenService.GenerateRefreshToken();
        var token2 = tokenService.GenerateRefreshToken();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token1));

        var bytes = Convert.FromBase64String(token1);
        
        Assert.Equal(64, bytes.Length);
        Assert.Equal(88, token1.Length);
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void Given_JwtSettingsWithExpiryMinutes_When_GetAccessTokenExpirationInSeconds_Then_ReturnsConfiguredSeconds()
    {
        // Arrange
        var configuration = CrateInMemoryConfiguration(); // ExpiryMinutes = 60
        var tokenService = new TokenService(configuration);

        // Act
        var seconds = tokenService.GetAccessTokenExpirationInSeconds();

        // Assert
        Assert.Equal(60 * 60, seconds);
    }

    [Fact]
    public void Given_JwtSettingsWithoutExpiryMinutes_When_GetAccessTokenExpirationInSeconds_Then_ReturnsDefault15Minutes()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"JwtSettings:Key", "key"},
            {"JwtSettings:Issuer", "issuer"},
            {"JwtSettings:Audience", "audience"}
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var tokenService = new TokenService(configuration);

        // Act
        var seconds = tokenService.GetAccessTokenExpirationInSeconds();

        // Assert
        // default is 15 minutes
        Assert.Equal(15 * 60, seconds);
    }

    [Fact]
    public void Given_JwtSettingsWithRefreshTokenExpirationDays_When_GetRefreshTokenExpirationDate_Then_ReturnsNowPlusConfiguredDays()
    {
        // Arrange: set refresh token expiration days to 10
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"JwtSettings:RefreshTokenExpirationDays", "10"},
            {"JwtSettings:Key", "key"},
            {"JwtSettings:Issuer", "issuer"},
            {"JwtSettings:Audience", "audience"}
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var tokenService = new TokenGenerators.TokenService(configuration);

        // Act
        var expiration = tokenService.GetRefreshTokenExpirationDate();

        // Assert
        // now + 10 days 
        var expected = DateTime.UtcNow.AddDays(10);
        var diff = (expiration - expected).Duration();
        Assert.True(diff < TimeSpan.FromSeconds(2));
    }
    
    
}