using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Data;
using Backend.Features.Shared.IAM.Constants;
using Backend.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Shared.IAM.VerifyPasswordReset;

public class VerifyPasswordResetHandler(
    UserManager<User> userManager,
    ApplicationContext context,
    IConfiguration configuration) : IRequestHandler<VerifyPasswordResetRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<VerifyPasswordResetHandler>();
    
    public async Task<IResult> Handle(VerifyPasswordResetRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to verify password reset token for user {UserId}", request.UserId);
        
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        
        if (user == null)
        {
            _logger.Warning("User {UserId} not found during password reset verification", request.UserId);
            return Results.BadRequest(new { error = "User not found" });
        }

        var now = DateTime.UtcNow;
        
        // Find the token in our database
        var storedToken = await context.PasswordResetTokens
            .Where(t => t.UserId == user.Id 
                     && !t.IsUsed 
                     && t.Code == request.Code
                     && t.ExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (storedToken == null)
        {
            _logger.Warning("Invalid or expired password reset token for user {UserId}", request.UserId);
            return Results.BadRequest(new { error = "Invalid or expired password reset token" });
        }

        // Verify the token with UserManager to ensure it's valid
        var isValidToken = await userManager.VerifyUserTokenAsync(
            user, 
            userManager.Options.Tokens.PasswordResetTokenProvider, 
            "ResetPassword", 
            request.Code);

        if (!isValidToken)
        {
            _logger.Warning("Token validation failed for user {UserId}", request.UserId);
            return Results.BadRequest(new { error = "Invalid password reset token" });
        }

        _logger.Information("Password reset token verified for user {UserId}, generating temporary JWT", request.UserId);
        
        // Mark token as used
        storedToken.IsUsed = true;
        await context.SaveChangesAsync(cancellationToken);

        // Generate a temporary short-lived JWT token for password change
        var tempToken = GenerateTemporaryToken(user);
        
        _logger.Information("Temporary password reset JWT generated for user {UserId}", request.UserId);
        
        return Results.Ok(new { 
            message = "Password reset token verified successfully",
            temporaryToken = tempToken,
            expiresInMinutes = IamConstants.ResetPasswordRightExpiryMinutes
        });
    }

    private string GenerateTemporaryToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"]!);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("password_reset", "true")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(IamConstants.ResetPasswordRightExpiryMinutes),
            Issuer = configuration["JwtSettings:Issuer"],
            Audience = configuration["JwtSettings:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
