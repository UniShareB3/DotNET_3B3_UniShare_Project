using Backend.Data;
using Backend.Features.Shared.IAM.Constants;
using Backend.Persistence;
using Backend.Services.EmailSender;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Shared.IAM.RequestPasswordReset;

public class RequestPasswordResetHandler(
    UserManager<User> userManager,
    ApplicationContext context,
    IEmailSender emailSender) : IRequestHandler<RequestPasswordResetRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<RequestPasswordResetHandler>();
    
    public async Task<IResult> Handle(RequestPasswordResetRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Processing password reset request for email {Email}", request.Email);

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

        // Remove any existing unused tokens for this user BEFORE generating new token
        var existingTokens = await context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed)
            .ToListAsync(cancellationToken);

        if (existingTokens.Count != 0)
        {
            context.PasswordResetTokens.RemoveRange(existingTokens);
            await context.SaveChangesAsync(cancellationToken);
        }

        // Update security stamp to ensure a new token is generated
        await userManager.UpdateSecurityStampAsync(user);

        // Reload user from database to ensure we have the updated security stamp
        // This is necessary because the in-memory user object may not reflect the new stamp
        user = await userManager.FindByIdAsync(user.Id.ToString())
               ?? throw new InvalidOperationException("User not found after security stamp update");

        // Generate password reset token using UserManager
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

        // Store the token in the database with expiration
        var passwordResetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Code = resetToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(IamConstants.ResetPasswordTokenExpiryMinutes),
            CreatedAt = DateTime.UtcNow
        };

        context.PasswordResetTokens.Add(passwordResetToken);
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            await emailSender.SendPasswordResetEmailAsync(user.Email, resetToken, user.Id);
            _logger.Information("Password reset email sent successfully to user {UserId}", user.Id);
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
