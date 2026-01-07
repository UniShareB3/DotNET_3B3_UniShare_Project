﻿using System.IdentityModel.Tokens.Jwt;
using Backend.Data;
using Backend.Services.Token;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

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
        var tokenService = new TokenService(configuration);
        var user = new User
        {
            Id = Guid.Parse("cb397a9b-ec7c-4bb4-b683-363f07dd94d6"),
            Email = "email@student.uaic.ro",
        };
        var roles = new List<string>(); // No roles needed for this test
        
        // Act
        var token = tokenService.GenerateToken(user, roles);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        
        // Assert
        userIdClaim.Should().NotBeNull();
        userIdClaim.Value.Should().Be(user.Id.ToString());
        emailClaim.Should().NotBeNull();
        emailClaim.Value.Should().Be(user.Email);
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
        token1.Should().NotBeNullOrWhiteSpace();

        var bytes = Convert.FromBase64String(token1);
        
        bytes.Length.Should().Be(64);
        token1.Length.Should().Be(88);
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void Given_JwtSettingsWithExpiryMinutes_When_GetAccessTokenExpirationInSeconds_Then_ReturnsConfiguredSeconds()
    {
        // Arrange
        var configuration = CrateInMemoryConfiguration(); // Expire = 900
        var tokenService = new TokenService(configuration);

        // Act
        var seconds = tokenService.GetAccessTokenExpirationInSeconds();

        // Assert
        seconds.Should().Be(30 * 30);
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
        seconds.Should().Be(15 * 60);
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
        var tokenService = new TokenService(configuration);

        // Act
        var expiration = tokenService.GetRefreshTokenExpirationDate();

        // Assert
        // now + 10 days 
        var expected = DateTime.UtcNow.AddDays(10);
        var diff = (expiration - expected).Duration();
        diff.Should().BeLessThan(TimeSpan.FromSeconds(2));
    }
    
    
}