using AutoMapper;
using Backend.Data;
using Backend.Features.ModeratorAssignment.DTO;
using Backend.Features.ModeratorAssignment.Enums;
using Backend.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;
using Backend.Features.Reports.Enums;

namespace Backend.Features.ModeratorAssignment.UpdateModeratorAssignment;

public class UpdateModeratorAssignmentStatusHandler(
    ApplicationContext context,
    IMapper mapper,
    UserManager<User> userManager)
    : IRequestHandler<UpdateModeratorAssignmentStatusRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<UpdateModeratorAssignmentStatusHandler>();

    public async Task<IResult> Handle(UpdateModeratorAssignmentStatusRequest request, CancellationToken cancellationToken)
{
    _logger.Information("Updating moderator assignment {AssignmentId} status to {Status}", 
        request.AssignmentId, request.Dto.Status);

    var moderatorAssignment = await context.ModeratorAssignments
        .FirstOrDefaultAsync(mr => mr.Id == request.AssignmentId, cancellationToken);

    if (moderatorAssignment == null)
    {
        _logger.Warning("Moderator assignment {AssignmentId} not found", request.AssignmentId);
        return Results.NotFound(new { message = "Moderator assignment not found" });
    }

    // Optimization: Check existence efficiently without fetching the whole entity
    var adminExists = await context.Users
        .AnyAsync(u => u.Id == request.Dto.ReviewedByAdminId, cancellationToken);

    if (!adminExists)
    {
        _logger.Warning("Admin {AdminId} not found", request.Dto.ReviewedByAdminId);
        return Results.NotFound(new { message = "Admin not found" });
    }

    // 1. Update the assignment entity
    moderatorAssignment.Status = request.Dto.Status;
    moderatorAssignment.ReviewedByAdminId = request.Dto.ReviewedByAdminId;
    moderatorAssignment.ReviewedDate = DateTime.UtcNow;

    // 2. Delegate "Accepted" logic to a helper method
    if (request.Dto.Status == ModeratorAssignmentStatus.Accepted)
    {
        await ProcessAcceptedAssignmentAsync(moderatorAssignment, cancellationToken);
    }

    await context.SaveChangesAsync(cancellationToken);

    var assignmentDto = mapper.Map<ModeratorAssignmentDto>(moderatorAssignment);
    _logger.Information("Moderator assignment {AssignmentId} status updated to {Status}", 
        request.AssignmentId, request.Dto.Status);

    return Results.Ok(assignmentDto);
}

// --- Helper Methods ---

private async Task ProcessAcceptedAssignmentAsync(Data.ModeratorAssignment assignment, CancellationToken ct)
{
    var user = await context.Users.FirstOrDefaultAsync(u => u.Id == assignment.UserId, ct);
    if (user == null) return;

    var hasModeratorRole = await userManager.IsInRoleAsync(user, "Moderator");
    if (hasModeratorRole) return;

    var result = await userManager.AddToRoleAsync(user, "Moderator");
    if (result.Succeeded)
    {
        _logger.Information("Assigned Moderator role to user {UserId}", user.Id);
        // Delegate the side-effect (reassigning reports) to its own isolated method
        await ReassignPendingReportsToNewModeratorAsync(user.Id, ct);
    }
    else
    {
        _logger.Error("Failed to assign Moderator role to user {UserId}: {Errors}", 
            user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}

private async Task ReassignPendingReportsToNewModeratorAsync(Guid newModeratorId, CancellationToken ct)
{
    try
    {
        var adminRoleId = await context.Roles
            .Where(r => r.Name == "Admin")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);

        if (adminRoleId == null) return;

        var adminUserIds = await context.UserRoles
            .Where(ur => ur.RoleId == adminRoleId)
            .Select(ur => ur.UserId)
            .ToListAsync(ct);

        if (adminUserIds.Count == 0) return;

        // Select up to 5 oldest pending reports currently assigned to admins
        var pendingAdminReports = await context.Reports
            .Where(r => r.Status == ReportStatus.Pending 
                     && r.ModeratorId != null 
                     && adminUserIds.Contains(r.ModeratorId.Value))
            .OrderBy(r => r.CreatedDate)
            .Take(5)
            .ToListAsync(ct);

        if (pendingAdminReports.Count == 0) return;

        foreach (var rep in pendingAdminReports)
        {
            rep.ModeratorId = newModeratorId;
        }

        _logger.Information("Reassigned {Count} pending reports to new moderator {UserId}", 
            pendingAdminReports.Count, newModeratorId);
    }
    catch (Exception ex)
    {
        // Log but do not crash the request, as this is a background optimization task
        _logger.Error(ex, "Failed to reassign pending reports to new moderator {UserId}", newModeratorId);
    }
}
}
