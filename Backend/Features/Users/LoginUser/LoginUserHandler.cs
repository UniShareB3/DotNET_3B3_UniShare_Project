using Microsoft.AspNetCore.Identity;
using Backend.Persistence;
using Backend.Data;
using Backend.TokenGenerators;
using Backend.Features.Users.Dtos;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Users;

public class LoginUserHandler(
    UserManager<User> userManager, 
    ITokenService tokenService,
    ApplicationContext context) : IRequestHandler<LoginUserRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<LoginUserHandler>();

    public async Task<IResult> Handle(LoginUserRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting login for email: {Email}", request.Email);
        
        var user = await userManager.FindByEmailAsync(request.Email);
        
        if (user == null)
        {
            _logger.Warning("Login failed: User not found for email {Email}", request.Email);
            return Results.Unauthorized();
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            _logger.Warning("Login failed: Invalid password for user {UserId}", user.Id);
            return Results.Unauthorized();
        }
        
        _logger.Information("User {UserId} authenticated successfully, generating tokens", user.Id);
        
        var existingTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == user.Id)
            .ToListAsync(cancellationToken);
        
        if (existingTokens.Any())
        {
            _logger.Information("Removing {TokenCount} existing refresh tokens for user {UserId}", 
                existingTokens.Count, user.Id);
            context.RefreshTokens.RemoveRange(existingTokens);
        }

        // Get user roles
        var roles = await userManager.GetRolesAsync(user);
        _logger.Information("User {UserId} has roles: {Roles}", user.Id, string.Join(", ", roles));
        
        var accessToken = tokenService.GenerateToken(user, roles);
        var refreshTokenString = tokenService.GenerateRefreshToken();
        
        var tokenFamily = Guid.NewGuid();
        
        var refreshToken = new RefreshToken
        {
            Token = refreshTokenString,
            UserId = user.Id,
            ExpiresAt = tokenService.GetRefreshTokenExpirationDate(),
            TokenFamily = tokenFamily,
            ParentTokenId = null,
            ReplacedByTokenId = null
        };
        
        context.RefreshTokens.Add(refreshToken);
        
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.Information("Refresh token created successfully for user {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save refresh token for user {UserId}", user.Id);
            throw;
        }
        
        var response = new LoginUserResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshTokenString,
            ExpiresIn: tokenService.GetAccessTokenExpirationInSeconds()
        );
        
        _logger.Information("Login successful for user {UserId}", user.Id);
        
        return Results.Ok(response);
    }
}