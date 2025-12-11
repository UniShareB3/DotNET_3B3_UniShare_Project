using Backend.Data;
using Backend.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Shared.Auth;

public class ChangePasswordHandler(
    UserManager<User> userManager,
    ApplicationContext context) : IRequestHandler<ChangePasswordRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<ChangePasswordHandler>();

    public async Task<IResult> Handle(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var dto = request.ChangePasswordDto;
        _logger.Information("Attempting to change password for user {UserId}", dto.UserId);

        var user = await userManager.FindByIdAsync(dto.UserId.ToString());
        
        if (user == null)
        {
            _logger.Warning("Password change failed: User {UserId} not found.", dto.UserId);
            return Results.NotFound(new { error = "User not found" });
        }

        // Find the most recent used password reset token for this user
        // (it was marked as used when they verified it in VerifyPasswordResetHandler)
        var recentToken = await context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.IsUsed)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        string resetToken;
        
        if (recentToken != null && recentToken.CreatedAt > DateTime.UtcNow.AddMinutes(-10))
        {
            // Use the stored token if it was created recently (within 10 minutes)
            resetToken = recentToken.Code;
            _logger.Information("Using stored password reset token for user {UserId}", dto.UserId);
        }
        else
        {
            // Generate a new token (for admin password resets or if token expired)
            resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            _logger.Information("Generated new password reset token for user {UserId}", dto.UserId);
        }

        var result = await userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);
        
        if (!result.Succeeded)
        {
            _logger.Error("Password change failed for {UserId}. Errors: {Errors}", 
                dto.UserId, 
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        // Clean up used tokens
        if (recentToken != null)
        {
            context.PasswordResetTokens.Remove(recentToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        _logger.Information("Password changed successfully for user {UserId}", dto.UserId);
        
        return Results.Ok(new { message = "Password changed successfully" });
    }
}
