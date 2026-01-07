using Backend.Data;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Shared.IAM.AssignAdminRole;

public class AssignAdminRoleHandler(UserManager<User> userManager) : IRequestHandler<AssignAdminRoleRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<AssignAdminRoleHandler>();
    
    public async Task<IResult> Handle(AssignAdminRoleRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to assign admin role to user with ID: {UserId}", request.UserId);
        
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        
        if (user == null)
        {
            _logger.Warning("Failed to assign admin role: User with ID {UserId} not found", request.UserId);
            return Results.NotFound(new { error = "User not found" });
        }

        var isAlreadyAdmin = await userManager.IsInRoleAsync(user, "Admin");
        if (isAlreadyAdmin)
        {
            _logger.Warning("User {UserId} is already assigned the admin role", request.UserId);
            return Results.BadRequest(new { error = "User is already an admin" });
        }

        var result = await userManager.AddToRoleAsync(user, "Admin");
        
        if (!result.Succeeded)
        {
            _logger.Error("Failed to assign admin role to user {UserId}. Errors: {Errors}", 
                request.UserId, 
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return Results.BadRequest(result.Errors);
        }

        _logger.Information("Successfully assigned admin role to user {UserId}", request.UserId);
        return Results.Ok(new { message = "Admin role assigned successfully", userId = user.Id });
    }
}
