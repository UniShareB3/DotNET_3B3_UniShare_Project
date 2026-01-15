using System.Security.Cryptography;
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

        // Generate a cryptographically secure random token
        // This bypasses ASP.NET Identity's DataProtectorTokenProvider which can have issues
        // with token uniqueness due to Data Protection key management and security stamp synchronization
        var resetToken = GenerateSecureRandomToken();

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

    /// <summary>
    /// Generates a cryptographically secure random token for password reset.
    /// Uses 32 bytes (256 bits) of randomness, encoded as URL-safe Base64.
    /// </summary>
    private static string GenerateSecureRandomToken()
    {
        var tokenBytes = new byte[32];
        RandomNumberGenerator.Fill(tokenBytes);
        // Use URL-safe Base64 encoding (replace + with -, / with _, remove padding =)
        return Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
