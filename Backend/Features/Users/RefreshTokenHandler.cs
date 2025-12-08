using Backend.Data;
using Backend.Persistence;
using Backend.TokenGenerators;
using Backend.Features.Users.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace Backend.Features.Users;

public class RefreshTokenHandler(
    UserManager<User> userManager, 
    ITokenService tokenService,
    ApplicationContext context) : IRequestHandler<RefreshTokenRequest, IResult>
{
    public async Task<IResult> Handle(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var storedRefreshToken = await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);
        
        if (storedRefreshToken == null)
        {
            return Results.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(storedRefreshToken.UserId.ToString());
        
        if (user == null)
        {
            return Results.Unauthorized();
        }

        if (storedRefreshToken.IsExpired)
        {
            return Results.Unauthorized();
        }

        if (storedRefreshToken.IsRevoked)
        {
            await RevokeTokenFamily(storedRefreshToken.TokenFamily, user.Id, "Token reuse detected - potential security breach", cancellationToken);
            return Results.Unauthorized();
        }

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
        
        var response = new LoginUserResponseDto(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshTokenString,
            ExpiresIn: tokenService.GetAccessTokenExpirationInSeconds()
        );
        
        return Results.Ok(response);
    }
    
    private async Task RevokeTokenFamily(Guid tokenFamily, Guid userId, string reason, CancellationToken cancellationToken)
    {
        var familyTokens = await context.RefreshTokens
            .Where(rt => rt.TokenFamily == tokenFamily && rt.UserId == userId)
            .ToListAsync(cancellationToken);
        
        foreach (var token in familyTokens) {
            if (!token.IsRevoked) {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.ReasonRevoked = reason;
            }
        }
        
        await context.SaveChangesAsync(cancellationToken);
    }
}