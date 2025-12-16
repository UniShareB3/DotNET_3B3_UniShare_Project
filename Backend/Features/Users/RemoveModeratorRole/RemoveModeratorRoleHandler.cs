using Backend.Data;
using Backend.Features.Reports.Enums;
using Backend.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Users.RemoveModeratorRole;

public class RemoveModeratorRoleHandler : IRequestHandler<RemoveModeratorRoleRequest, IResult>
{
    private readonly ApplicationContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger _logger = Log.ForContext<RemoveModeratorRoleHandler>();

    public RemoveModeratorRoleHandler(ApplicationContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IResult> Handle(RemoveModeratorRoleRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Removing Moderator role from user {UserId}", request.UserId);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        
        if (user == null)
        {
            _logger.Warning("User {UserId} not found", request.UserId);
            return Results.NotFound(new { message = "User not found" });
        }

        var hasModeratorRole = await _userManager.IsInRoleAsync(user, "Moderator");
        
        if (!hasModeratorRole)
        {
            _logger.Information("User {UserId} does not have Moderator role", request.UserId);
            return Results.BadRequest(new { message = "User does not have the Moderator role" });
        }

        // Get all pending reports assigned to this moderator
        var pendingReports = await _context.Reports
            .Where(r => r.ModeratorId == request.UserId && r.Status == ReportStatus.PENDING)
            .ToListAsync(cancellationToken);

        if (pendingReports.Any())
        {
            _logger.Information("Found {Count} pending reports assigned to user {UserId}, reassigning them", 
                pendingReports.Count, request.UserId);

            // Get all other moderators (excluding this user)
            var allUsers = await _context.Users.ToListAsync(cancellationToken);
            var otherModerators = new List<User>();
            
            foreach (var u in allUsers)
            {
                if (u.Id == request.UserId) continue;
                
                var roles = await _userManager.GetRolesAsync(u);
                if (roles.Contains("Moderator"))
                {
                    otherModerators.Add(u);
                }
            }

            // If no other moderators exist, find the admin
            if (!otherModerators.Any())
            {
                _logger.Information("No other moderators found, assigning reports to admin");
                
                foreach (var u in allUsers)
                {
                    var roles = await _userManager.GetRolesAsync(u);
                    if (roles.Contains("Admin"))
                    {
                        otherModerators.Add(u);
                        break;
                    }
                }
            }

            // Reassign reports
            if (otherModerators.Any())
            {
                var random = new Random();
                foreach (var report in pendingReports)
                {
                    var assignedModerator = otherModerators[random.Next(otherModerators.Count)];
                    report.ModeratorId = assignedModerator.Id;
                    _logger.Information("Reassigned report {ReportId} to moderator {ModeratorId}", 
                        report.Id, assignedModerator.Id);
                }
                
                await _context.SaveChangesAsync(cancellationToken);
                _logger.Information("Successfully reassigned {Count} pending reports", pendingReports.Count);
            }
            else
            {
                _logger.Warning("No moderators or admin found to reassign reports to");
            }
        }

        // Remove the Moderator role
        var result = await _userManager.RemoveFromRoleAsync(user, "Moderator");
        
        if (!result.Succeeded)
        {
            _logger.Error("Failed to remove Moderator role from user {UserId}: {Errors}", 
                request.UserId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Results.BadRequest(new { 
                message = "Failed to remove Moderator role", 
                errors = result.Errors 
            });
        }

        _logger.Information("Successfully removed Moderator role from user {UserId}", request.UserId);
        return Results.Ok(new { 
            message = "Moderator role removed successfully", 
            userId = request.UserId,
            reassignedReportsCount = pendingReports.Count
        });
    }
}

