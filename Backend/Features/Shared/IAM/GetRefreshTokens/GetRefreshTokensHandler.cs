using Backend.Data;
using Backend.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Shared.IAM.GetRefreshTokens;

public class GetRefreshTokensHandler(
    UserManager<User> userManager,
    ApplicationContext context) : IRequestHandler<GetRefreshTokensRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GetRefreshTokensHandler>();
    
    public async Task<IResult> Handle(GetRefreshTokensRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving refresh tokens for user {UserId}", request.UserId);
        
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        
        if (user == null)
        {
            _logger.Warning("User with ID {UserId} not found when retrieving refresh tokens", request.UserId);
            return Results.NotFound(new { message = "User not found" });
        }

        var refreshTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == user.Id)
            .OrderByDescending(rt => rt.CreatedAt)
            .Select(rt => new
            {
                rt.Id,
                rt.Token,
                rt.CreatedAt,
                rt.ExpiresAt,
                rt.IsExpired,
                rt.IsRevoked,
                rt.RevokedAt,
                rt.ReasonRevoked,
                rt.TokenFamily,
                rt.ParentTokenId,
                rt.ReplacedByTokenId
            })
            .ToListAsync(cancellationToken);

        _logger.Information("Retrieved {TokenCount} refresh tokens for user {UserId}", refreshTokens.Count, request.UserId);
        
        return Results.Ok(new
        {
            userEmail = user.Email,
            userId = user.Id,
            tokens = refreshTokens
        });
    }
}
