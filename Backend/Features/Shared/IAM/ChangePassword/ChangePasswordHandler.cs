﻿using Backend.Data;
 using Backend.Features.Shared.IAM.Constants;
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
        
        var recentToken = await context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (recentToken == null || recentToken.CreatedAt < DateTime.UtcNow.AddMinutes(-IAMConstants.ResetPasswordTokenExpiryMinutes))
        {
            _logger.Warning("No valid password reset token found for user {UserId}", dto.UserId);
            return Results.BadRequest(new { error = "Password reset token expired or not found. Please request a new password reset." });
        }

        recentToken.IsUsed = true;

        string resetToken = recentToken.Code;
        _logger.Information("Using stored password reset token for user {UserId}", dto.UserId);

        var result = await userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);
        
        if (!result.Succeeded)
        {
            _logger.Error("Password change failed for {UserId}. Errors: {Errors}", 
                dto.UserId, 
                string.Join(", ", result.Errors.Select(e => e.Description)));

            // Return validation errors in a format that frontend can use
            var errors = new Dictionary<string, List<string>>();
            foreach (var error in result.Errors)
            {
                if (!errors.ContainsKey(error.Code))
                {
                    errors[error.Code] = new List<string>();
                }
                errors[error.Code].Add(error.Description);
            }

            return Results.BadRequest(errors);
        }

        // Clean up used token
        context.PasswordResetTokens.Remove(recentToken);
        await context.SaveChangesAsync(cancellationToken);

        _logger.Information("Password changed successfully for user {UserId}", dto.UserId);
        
        return Results.Ok(new { message = "Password changed successfully" });
    }
}
