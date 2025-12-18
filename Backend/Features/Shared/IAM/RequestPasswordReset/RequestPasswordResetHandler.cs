using Backend.Data;
using Backend.Features.Shared.IAM.Constants;
using Backend.Persistence;
using Backend.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Shared.Auth;

public class RequestPasswordResetHandler(
    UserManager<User> userManager,
    ApplicationContext context,
    IEmailSender emailSender) : IRequestHandler<RequestPasswordResetRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<RequestPasswordResetHandler>();
    
    public async Task<IResult> Handle(RequestPasswordResetRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to send password reset token to email {Email}", request.Email);

        var user = await userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            _logger.Warning("User with email {Email} not found for password reset", request.Email);
            return Results.NotFound(new { error = "User not found" });
        }

        if (user.Email == null)
        {
            _logger.Warning("User {UserId} has no email address", user.Id);
            return Results.BadRequest(new { error = "User has no email address" });
        }

        // Generate password reset token using UserManager
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        
        _logger.Information("Generated password reset token for user {UserId}", user.Id);

        // Store the token in the database with expiration
        var passwordResetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Code = resetToken, // Store the actual token
            ExpiresAt = DateTime.UtcNow.AddMinutes(IAMConstants.ResetPasswordTokenExpiryMinutes),
            CreatedAt = DateTime.UtcNow
        };

        // Remove any existing unused tokens for this user
        var existingTokens = await context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed)
            .ToListAsync(cancellationToken);
        
        if (existingTokens.Any())
        {
            _logger.Information("Removing {TokenCount} existing unused password reset tokens for user {UserId}", 
                existingTokens.Count, user.Id);
            context.PasswordResetTokens.RemoveRange(existingTokens);
        }

        context.PasswordResetTokens.Add(passwordResetToken);
        await context.SaveChangesAsync(cancellationToken);
        
        _logger.Information("Saved password reset token for user {UserId}, expires at {ExpiresAt}", 
            user.Id, passwordResetToken.ExpiresAt);

        try
        {
            await emailSender.SendPasswordResetEmailAsync(user.Email, resetToken, user.Id);
            _logger.Information("Password reset email sent successfully to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to send password reset email to {Email}", user.Email);
            return Results.Problem("Failed to send password reset email");
        }
        
        return Results.Ok(new { 
            message = "Password reset token sent successfully",
            expiresInMinutes = 15
        });
    }
}
