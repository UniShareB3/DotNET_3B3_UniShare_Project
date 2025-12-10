using Backend.Data;
using Backend.Persistence;
using Backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Users;

public class SendEmailVerificationHandler(
    UserManager<User> userManager,
    ApplicationContext context,
    IEmailSender emailSender,
    IHashingService hashingService) : IRequestHandler<SendEmailVerificationRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<SendEmailVerificationHandler>();
    
    public async Task<IResult> Handle(SendEmailVerificationRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Sending email verification for user {UserId}", request.UserId);
        
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        
        if (user == null)
        {
            _logger.Warning("User with ID {UserId} not found for email verification", request.UserId);
            return Results.BadRequest(new { error = "User not found" });
        }
        
        if (user.EmailConfirmed)
        {
            _logger.Warning("Email already confirmed for user {UserId}", request.UserId);
            return Results.BadRequest(new { error = "Email already confirmed" });
        }

        var existingTokens = await context.EmailConfirmationTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed)
            .ToListAsync(cancellationToken);
        
        if (existingTokens.Any())
        {
            _logger.Information("Removing {TokenCount} existing unused email confirmation tokens for user {UserId}", 
                existingTokens.Count, user.Id);
            context.EmailConfirmationTokens.RemoveRange(existingTokens);
        }
        
        var code = new Random().Next(100000, 999999).ToString();
        var hashedCode = hashingService.HashCode(code);
        var verificationToken = new EmailConfirmationToken
        {
            UserId = user.Id,
            Code = hashedCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false
        };

        context.EmailConfirmationTokens.Add(verificationToken);
        await context.SaveChangesAsync(cancellationToken);
        
        _logger.Information("Created new email confirmation token for user {UserId}", user.Id);

        var subject = "Email Verification Code - UniShare";
        var body = $@"Hello {user.FirstName},

                    Your email verification code is: {code}

                    This code will expire in 5 minutes.

                    If you didn't request this code, please ignore this email.

                    Best regards,
                    UniShare Team";

        try
        {
            await emailSender.SendEmailAsync(user.Email!, subject, body);
            _logger.Information("Successfully sent verification email to user {UserId}", user.Id);
            return Results.Ok(new { message = "Verification code sent to your email" });
        }
        catch (Exception ex) {
            _logger.Error(ex, "Failed to send verification email to user {UserId}", user.Id);
            return Results.Problem($"Failed to send email: {ex.Message}");
        }
    }
}
