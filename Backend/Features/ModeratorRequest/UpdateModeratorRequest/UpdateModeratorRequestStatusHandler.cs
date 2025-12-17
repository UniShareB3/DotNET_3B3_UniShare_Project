using AutoMapper;
using Backend.Data;
using Backend.Features.ModeratorRequest.DTO;
using Backend.Features.ModeratorRequest.Enums;
using Backend.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;
using Backend.Features.Reports.Enums;

namespace Backend.Features.ModeratorRequest.UpdateModeratorRequest;

public class UpdateModeratorRequestStatusHandler : IRequestHandler<UpdateModeratorRequestStatusRequest, IResult>
{
    private readonly ApplicationContext _context;
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;
    private readonly ILogger _logger = Log.ForContext<UpdateModeratorRequestStatusHandler>();

    public UpdateModeratorRequestStatusHandler(ApplicationContext context, IMapper mapper, UserManager<User> userManager)
    {
        _context = context;
        _mapper = mapper;
        _userManager = userManager;
    }

    public async Task<IResult> Handle(UpdateModeratorRequestStatusRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Updating moderator request {RequestId} status to {Status}", 
            request.RequestId, request.Dto.Status);

        var moderatorRequest = await _context.ModeratorRequests
            .FirstOrDefaultAsync(mr => mr.Id == request.RequestId, cancellationToken);

        if (moderatorRequest == null)
        {
            _logger.Warning("Moderator request {RequestId} not found", request.RequestId);
            return Results.NotFound(new { message = "Moderator request not found" });
        }

        // Verify admin exists
        var admin = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Dto.ReviewedByAdminId, cancellationToken);

        if (admin == null)
        {
            _logger.Warning("Admin {AdminId} not found", request.Dto.ReviewedByAdminId);
            return Results.NotFound(new { message = "Admin not found" });
        }

        moderatorRequest.Status = request.Dto.Status;
        moderatorRequest.ReviewedByAdminId = request.Dto.ReviewedByAdminId;
        moderatorRequest.ReviewedDate = DateTime.UtcNow;

        // If status is ACCEPTED, assign Moderator role to the user
        if (request.Dto.Status == ModeratorRequestStatus.ACCEPTED)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == moderatorRequest.UserId, cancellationToken);

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

        var requestDto = _mapper.Map<ModeratorRequestDto>(moderatorRequest);
        _logger.Information("Moderator request {RequestId} status updated to {Status}", 
            request.RequestId, request.Dto.Status);

        return Results.Ok(requestDto);
    }
}
