using Backend.Data;
using Backend.Features.Shared.IAM.Constants;
using Backend.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Shared.IAM.ChangePassword;

public class ChangePasswordHandler(
    UserManager<User> userManager,
    ApplicationContext context) : IRequestHandler<ChangePasswordRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<ChangePasswordHandler>();
    
    public async Task<IResult> Handle(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var dto = request.ChangePasswordDto;
        _logger.Information("Processing password change request for user {UserId}", dto.UserId);

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

        if (recentToken == null || recentToken.CreatedAt < DateTime.UtcNow.AddMinutes(-IamConstants.ResetPasswordTokenExpiryMinutes))
        {
            _logger.Warning("No valid password reset token found for user {UserId}", dto.UserId);
            return Results.BadRequest(new { error = "Password reset token expired or not found. Please request a new password reset." });
        }

        // Validate the new password against Identity's password requirements
        var passwordValidators = userManager.PasswordValidators;
        foreach (var validator in passwordValidators)
        {
            var validationResult = await validator.ValidateAsync(userManager, user, dto.NewPassword);
            if (!validationResult.Succeeded)
            {
                _logger.Error("Password validation failed for {UserId}. Errors: {Errors}",
                    dto.UserId,
                    string.Join(", ", validationResult.Errors.Select(e => e.Description)));

                var errors = new Dictionary<string, List<string>>();
                foreach (var error in validationResult.Errors)
                {
                    if (!errors.TryGetValue(error.Code, out List<string>? value))
                    {
                        value = [];
                        errors[error.Code] = value;
                    }
                    value.Add(error.Description);
                }
                return Results.BadRequest(errors);
            }
        }

        // Remove the old password and set the new one
        // This bypasses the need for an Identity reset token since we've already validated
        // the user's identity through our custom token system
        var removeResult = await userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            _logger.Error("Failed to remove old password for {UserId}", dto.UserId);
            return Results.Problem("Failed to update password");
        }

        var addResult = await userManager.AddPasswordAsync(user, dto.NewPassword);
        if (!addResult.Succeeded)
        {
            _logger.Error("Password change failed for {UserId}. Errors: {Errors}",
                dto.UserId,
                string.Join(", ", addResult.Errors.Select(e => e.Description)));

            var errors = new Dictionary<string, List<string>>();
            foreach (var error in addResult.Errors)
            {
                if (!errors.TryGetValue(error.Code, out List<string>? value))
                {
                    value = [];
                    errors[error.Code] = value;
                }
                value.Add(error.Description);
            }
            return Results.BadRequest(errors);
        }

        // Mark token as used and clean up
        recentToken.IsUsed = true;
        context.PasswordResetTokens.Remove(recentToken);
        await context.SaveChangesAsync(cancellationToken);

        _logger.Information("Password changed successfully for user {UserId}", dto.UserId);
        
        return Results.Ok(new { message = "Password changed successfully" });
    }
}
