using Backend.Data;
using Backend.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Users.AssignModeratorRole;

public class AssignModeratorRoleHandler : IRequestHandler<AssignModeratorRoleRequest, IResult>
{
    private readonly ApplicationContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger _logger = Log.ForContext<AssignModeratorRoleHandler>();

    public AssignModeratorRoleHandler(ApplicationContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IResult> Handle(AssignModeratorRoleRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Assigning Moderator role to user {UserId}", request.UserId);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        
        if (user == null)
        {
            _logger.Warning("User {UserId} not found", request.UserId);
            return Results.NotFound(new { message = "User not found" });
        }

        var hasModeratorRole = await _userManager.IsInRoleAsync(user, "Moderator");
        
        if (hasModeratorRole)
        {
            _logger.Information("User {UserId} already has Moderator role", request.UserId);
            return Results.Conflict(new { message = "User already has the Moderator role" });
        }

        var result = await _userManager.AddToRoleAsync(user, "Moderator");
        
        if (!result.Succeeded)
        {
            _logger.Error("Failed to assign Moderator role to user {UserId}: {Errors}", 
                request.UserId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Results.BadRequest(new { message = "Failed to assign Moderator role", errors = result.Errors });
        }

        _logger.Information("Successfully assigned Moderator role to user {UserId}", request.UserId);
        return Results.Ok(new { message = "Moderator role assigned successfully", userId = request.UserId });
    }
}

