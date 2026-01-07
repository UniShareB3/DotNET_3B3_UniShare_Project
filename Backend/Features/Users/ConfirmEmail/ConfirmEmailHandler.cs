using Backend.Data;
using Backend.Persistence;
using Backend.Services.Hashing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Users.ConfirmEmail;

public class ConfirmEmailHandler(
    UserManager<User> userManager,
    ApplicationContext context,
    IHashingService hashingService) : IRequestHandler<ConfirmEmailRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<ConfirmEmailHandler>();
    
    public async Task<IResult> Handle(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to confirm email for user {UserId}", request.UserId);
        
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        
        if (user == null)
        {
            _logger.Warning("User not found for ID {UserId} during email confirmation", request.UserId);
            return Results.BadRequest(new { error = "User not found" });
        }

        if (user.NewEmailConfirmed)
        {
            _logger.Warning("Email already confirmed for user {UserId}", request.UserId);
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
            _logger.Warning("Invalid or expired verification code for user {UserId}", request.UserId);
            return Results.BadRequest(new { error = "Invalid or expired verification code" });
        }

        _logger.Information("Confirming email for user {UserId}", request.UserId);
        
        token.IsUsed = true;
        user.NewEmailConfirmed = true;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            _logger.Error("Failed to update user {UserId} during email confirmation. Errors: {Errors}", 
                request.UserId, 
                string.Join(", ", updateResult.Errors.Select(e => e.Description)));
            return Results.Problem("Failed to confirm email");
        }
        
        await context.SaveChangesAsync(cancellationToken);
        
        _logger.Information("Email confirmed successfully for user {UserId}", request.UserId);
        return Results.Ok(new { message = "Email confirmed successfully" });
    }
}
