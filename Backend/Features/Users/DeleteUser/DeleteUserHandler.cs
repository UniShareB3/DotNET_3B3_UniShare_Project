using Backend.Data;
using Backend.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Users;

public class DeleteUserHandler(
    UserManager<User> userManager,
    ApplicationContext context) : IRequestHandler<DeleteUserRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<DeleteUserHandler>();
    
    public async Task<IResult> Handle(DeleteUserRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to delete user with ID: {UserId}", request.UserId);
        
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        
        if (user == null) 
        {
            _logger.Warning("User with ID {UserId} not found for deletion", request.UserId);
            return Results.NotFound(new { message = "User not found" });
        }

        var refreshTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == user.Id)
            .ToListAsync(cancellationToken);
        
        if (refreshTokens.Any()) 
        {
            _logger.Information("Removing {TokenCount} refresh tokens for user {UserId}", refreshTokens.Count, user.Id);
            context.RefreshTokens.RemoveRange(refreshTokens);
        }

        var emailTokens = await context.EmailConfirmationTokens
            .Where(et => et.UserId == user.Id)
            .ToListAsync(cancellationToken);
        
        if (emailTokens.Any()) {
            _logger.Information("Removing {TokenCount} email confirmation tokens for user {UserId}", emailTokens.Count, user.Id);
            context.EmailConfirmationTokens.RemoveRange(emailTokens);
        }

        var result = await userManager.DeleteAsync(user);
        
        if (!result.Succeeded)
        {
            _logger.Error("Failed to delete user {UserId}. Errors: {Errors}", 
                user.Id, 
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return Results.BadRequest(new
            {
                message = "Failed to delete user",
                errors = result.Errors
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        
        _logger.Information("Successfully deleted user {UserId}", user.Id);

        return Results.Ok(new
        {
            message = "User deleted successfully",
            userId = user.Id
        });
    }
}
