using Backend.Data;
using Backend.Features.Users.Dtos;
using Backend.Persistence;
using Backend.TokenGenerators;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Shared.Auth;

public class RefreshTokenHandler(
    UserManager<User> userManager, 
    ITokenService tokenService,
    ApplicationContext context) : IRequestHandler<RefreshTokenRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<RefreshTokenHandler>();
    
    public async Task<IResult> Handle(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to refresh token");
        
        var storedRefreshToken = await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);
        
        if (storedRefreshToken == null)
        {
            _logger.Warning("Refresh token not found in database");
            return Results.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(storedRefreshToken.UserId.ToString());
        
        if (user == null)
        {
            _logger.Warning("User {UserId} not found for refresh token", storedRefreshToken.UserId);
            return Results.Unauthorized();
        }

        if (storedRefreshToken.IsExpired)
        {
            _logger.Warning("Refresh token expired for user {UserId}", user.Id);
            return Results.Unauthorized();
        }

        if (storedRefreshToken.IsRevoked)
        {
            _logger.Warning("Token reuse detected for user {UserId}. Revoking token family {TokenFamily}", 
                user.Id, storedRefreshToken.TokenFamily);
            await RevokeTokenFamily(storedRefreshToken.TokenFamily, user.Id, "Token reuse detected - potential security breach", cancellationToken);
            return Results.Unauthorized();
        }

        _logger.Information("Revoking old refresh token and creating new one for user {UserId}", user.Id);
        
        storedRefreshToken.IsRevoked = true;
        storedRefreshToken.RevokedAt = DateTime.UtcNow;
        storedRefreshToken.ReasonRevoked = "Rotated to new token";
        
        // Get user roles
        var roles = await userManager.GetRolesAsync(user);
        
        var newAccessToken = tokenService.GenerateToken(user, roles);
        var newRefreshTokenString = tokenService.GenerateRefreshToken();
        
        var newRefreshToken = new RefreshToken
        {
            Token = newRefreshTokenString,
            UserId = user.Id,
            ExpiresAt = tokenService.GetRefreshTokenExpirationDate(),
            TokenFamily = storedRefreshToken.TokenFamily,
            ParentTokenId = storedRefreshToken.Id,
            ReplacedByTokenId = null
        };
        
        storedRefreshToken.ReplacedByTokenId = newRefreshToken.Id;
        
        context.RefreshTokens.Add(newRefreshToken);
        await context.SaveChangesAsync(cancellationToken);
        
        _logger.Information("Successfully refreshed token for user {UserId}", user.Id);
        
        var response = new LoginUserResponseDto(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshTokenString,
            ExpiresIn: tokenService.GetAccessTokenExpirationInSeconds()
        );
        
        return Results.Ok(response);
    }
    
    private async Task RevokeTokenFamily(Guid tokenFamily, Guid userId, string reason, CancellationToken cancellationToken)
    {
        _logger.Warning("Revoking entire token family {TokenFamily} for user {UserId}. Reason: {Reason}", 
            tokenFamily, userId, reason);
        
        var familyTokens = await context.RefreshTokens
            .Where(rt => rt.TokenFamily == tokenFamily && rt.UserId == userId)
            .ToListAsync(cancellationToken);
        
        _logger.Information("Found {TokenCount} tokens in family {TokenFamily} to revoke", familyTokens.Count, tokenFamily);
        
        foreach (var token in familyTokens) {
            if (!token.IsRevoked) {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.ReasonRevoked = reason;
            }
        }
        
        await context.SaveChangesAsync(cancellationToken);
        
        _logger.Information("Successfully revoked token family {TokenFamily} for user {UserId}", tokenFamily, userId);
    }
}