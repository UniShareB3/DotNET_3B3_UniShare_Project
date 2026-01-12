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

        // Generate password reset token using UserManager
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

        // Store the token in the database with expiration
        var passwordResetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Code = resetToken, // Store the actual token
            ExpiresAt = DateTime.UtcNow.AddMinutes(IamConstants.ResetPasswordTokenExpiryMinutes),
            CreatedAt = DateTime.UtcNow
        };

        // Remove any existing unused tokens for this user
        var existingTokens = await context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed)
            .ToListAsync(cancellationToken);

        if (existingTokens.Count != 0)
        {
            context.PasswordResetTokens.RemoveRange(existingTokens);
        }

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
