using Backend.Data;
using Backend.Persistence;
using Backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MediatR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Users;

public class ConfirmEmailHandler(
    UserManager<User> userManager,
    ApplicationContext context,
    IHashingService hashingService) : IRequestHandler<ConfirmEmailRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<ConfirmEmailHandler>();
    
    public async Task<IResult> Handle(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to confirm email with JWT token");
        
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(request.JwtToken))
        {
            _logger.Warning("Invalid JWT token provided for email confirmation");
            return Results.BadRequest(new { error = "Invalid JWT token" });
        }

        var jwtToken = handler.ReadJwtToken(request.JwtToken);
        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => 
            c.Type == ClaimTypes.NameIdentifier || 
            c.Type == JwtRegisteredClaimNames.Sub || 
            c.Type == "sub");

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.Warning("Invalid or missing UserId in JWT token");
            return Results.BadRequest(new { error = "Invalid or missing UserId in JWT token" });
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        
        if (user == null)
        {
            _logger.Warning("User not found for ID {UserId} during email confirmation", userId);
            return Results.BadRequest(new { error = "User not found" });
        }

        if (user.EmailConfirmed)
        {
            _logger.Warning("Email already confirmed for user {UserId}", userId);
            return Results.BadRequest(new { error = "Email already confirmed" });
        }

        var now = DateTime.UtcNow;

        var hashedCode = hashingService.HashCode(request.Code);
        
        var token = await context.EmailConfirmationTokens
            .Where(t => t.UserId == user.Id 
                     && !t.IsUsed 
                     && t.Code == hashedCode
                     && t.ExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (token == null)
        {
            _logger.Warning("Invalid or expired verification code for user {UserId}", userId);
            return Results.BadRequest(new { error = "Invalid or expired verification code" });
        }

        _logger.Information("Confirming email for user {UserId}", userId);
        
        token.IsUsed = true;
        user.EmailConfirmed = true;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            _logger.Error("Failed to update user {UserId} during email confirmation. Errors: {Errors}", 
                userId, 
                string.Join(", ", updateResult.Errors.Select(e => e.Description)));
            return Results.Problem("Failed to confirm email");
        }
        
        await context.SaveChangesAsync(cancellationToken);
        
        _logger.Information("Email confirmed successfully for user {UserId}", userId);
        return Results.Ok(new { message = "Email confirmed successfully" });
    }
}
