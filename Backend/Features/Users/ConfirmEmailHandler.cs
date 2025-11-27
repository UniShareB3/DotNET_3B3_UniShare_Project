using Backend.Data;
using Backend.Persistence;
using Backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MediatR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Backend.Features.Users;

public class ConfirmEmailHandler(
    UserManager<User> userManager,
    ApplicationContext context,
    IHashingService hashingService) : IRequestHandler<ConfirmEmailRequest, IResult>
{
    public async Task<IResult> Handle(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(request.JwtToken))
        {
            return Results.BadRequest(new { error = "Invalid JWT token" });
        }

        var jwtToken = handler.ReadJwtToken(request.JwtToken);
        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => 
            c.Type == ClaimTypes.NameIdentifier || 
            c.Type == JwtRegisteredClaimNames.Sub || 
            c.Type == "sub");

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.BadRequest(new { error = "Invalid or missing UserId in JWT token" });
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        
        if (user == null)
        {
            return Results.BadRequest(new { error = "User not found" });
        }

        if (user.EmailConfirmed)
        {
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
            return Results.BadRequest(new { error = "Invalid or expired verification code" });
        }

        token.IsUsed = true;

        user.EmailConfirmed = true;

        await userManager.UpdateAsync(user);
        await context.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { message = "Email confirmed successfully" });
    }
}
