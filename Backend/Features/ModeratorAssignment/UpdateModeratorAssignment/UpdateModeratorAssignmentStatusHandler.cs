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

public class UpdateModeratorAssignmentStatusHandler : IRequestHandler<UpdateModeratorAssignmentStatusRequest, IResult>
{
    private readonly ApplicationContext _context;
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;
    private readonly ILogger _logger = Log.ForContext<UpdateModeratorAssignmentStatusHandler>();

    public UpdateModeratorAssignmentStatusHandler(ApplicationContext context, IMapper mapper, UserManager<User> userManager)
    {
        _context = context;
        _mapper = mapper;
        _userManager = userManager;
    }

    public async Task<IResult> Handle(UpdateModeratorAssignmentStatusRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Updating moderator assignment {AssignmentId} status to {Status}", 
            request.AssignmentId, request.Dto.Status);

        var moderatorAssignment = await _context.ModeratorAssignments
            .FirstOrDefaultAsync(mr => mr.Id == request.AssignmentId, cancellationToken);

        if (moderatorAssignment == null)
        {
            _logger.Warning("Moderator assignment {AssignmentId} not found", request.AssignmentId);
            return Results.NotFound(new { message = "Moderator assignment not found" });
        }

        // Verify admin exists
        var admin = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Dto.ReviewedByAdminId, cancellationToken);

        if (admin == null)
        {
            _logger.Warning("Admin {AdminId} not found", request.Dto.ReviewedByAdminId);
            return Results.NotFound(new { message = "Admin not found" });
        }

        moderatorAssignment.Status = request.Dto.Status;
        moderatorAssignment.ReviewedByAdminId = request.Dto.ReviewedByAdminId;
        moderatorAssignment.ReviewedDate = DateTime.UtcNow;

        // If status is ACCEPTED, assign Moderator role to the user
        if (request.Dto.Status == ModeratorAssignmentStatus.ACCEPTED)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == moderatorAssignment.UserId, cancellationToken);

            if (user != null)
            {
                var hasModeratorRole = await _userManager.IsInRoleAsync(user, "Moderator");
                if (!hasModeratorRole)
                {
                    var result = await _userManager.AddToRoleAsync(user, "Moderator");
                    if (result.Succeeded)
                    {
                        _logger.Information("Assigned Moderator role to user {UserId}", user.Id);

                        // Reassign a few pending reports currently assigned to Admins to the newly promoted moderator
                        try
                        {
                            // Find admin role id
                            var adminRoleId = await _context.Roles
                                .Where(r => r.Name == "Admin")
                                .Select(r => r.Id)
                                .FirstOrDefaultAsync(cancellationToken);

                            if (adminRoleId != null)
                            {
                                // Get admin user ids
                                var adminUserIds = await _context.UserRoles
                                    .Where(ur => ur.RoleId == adminRoleId)
                                    .Select(ur => ur.UserId)
                                    .ToListAsync(cancellationToken);

                                if (adminUserIds.Any())
                                {
                                    // Select up to 5 oldest pending reports that are currently assigned to admins
                                    var pendingAdminReports = await _context.Reports
                                        .Where(r => r.Status == ReportStatus.PENDING && r.ModeratorId != null && adminUserIds.Contains(r.ModeratorId.Value))
                                        .OrderBy(r => r.CreatedDate)
                                        .Take(5)
                                        .ToListAsync(cancellationToken);

                                    foreach (var rep in pendingAdminReports)
                                    {
                                        rep.ModeratorId = user.Id;
                                    }

                                    if (pendingAdminReports.Any())
                                    {
                                        _logger.Information("Reassigned {Count} pending reports to new moderator {UserId}", pendingAdminReports.Count, user.Id);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Failed to reassign pending reports to new moderator {UserId}", user.Id);
                        }
                    }
                    else
                    {
                        _logger.Error("Failed to assign Moderator role to user {UserId}: {Errors}", 
                            user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var assignmentDto = _mapper.Map<ModeratorAssignmentDto>(moderatorAssignment);
        _logger.Information("Moderator assignment {AssignmentId} status updated to {Status}", 
            request.AssignmentId, request.Dto.Status);

        return Results.Ok(assignmentDto);
    }
}
