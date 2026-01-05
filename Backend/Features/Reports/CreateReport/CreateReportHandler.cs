using AutoMapper;
using Backend.Data;
using Backend.Features.Reports.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;
using Backend.Features.Reports.Enums;

namespace Backend.Features.Reports.CreateReport;

public class CreateReportHandler : IRequestHandler<CreateReportRequest, IResult>
{
    private readonly ApplicationContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger _logger = Log.ForContext<CreateReportHandler>();

    public CreateReportHandler(ApplicationContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IResult> Handle(CreateReportRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Creating report for item {ItemId} by user {UserId}", 
            request.Dto.ItemId, request.Dto.UserId);

        // Verify item exists
        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == request.Dto.ItemId, cancellationToken);

        if (item == null)
        {
            _logger.Warning("Item {ItemId} not found", request.Dto.ItemId);
            return Results.NotFound(new { message = "Item not found" });
        }

        // Verify user exists
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Dto.UserId, cancellationToken);

        if (user == null)
        {
            _logger.Warning("User {UserId} not found", request.Dto.UserId);
            return Results.NotFound(new { message = "User not found" });
        }

        var report = _mapper.Map<Data.Report>(request.Dto);
        report.OwnerId = item.OwnerId;
        report.CreatedDate = DateTime.UtcNow;
        report.Status = ReportStatus.PENDING;
        
        // Select a Moderator (user in 'Moderator' role) with the least number of PENDING reports assigned.
        // This ensures we distribute new reports to moderators first. If there are no moderators, fallback to a random Admin.

        Guid? selectedModeratorId = await allocateModerator(cancellationToken);

        report.ModeratorId = selectedModeratorId;
        _logger.Information("Selected moderator for new report: {ModeratorId}", selectedModeratorId?.ToString() ?? "<none>");

        _context.Reports.Add(report);
        await _context.SaveChangesAsync(cancellationToken);

        var reportDto = _mapper.Map<ReportDto>(report);
        _logger.Information("Report {ReportId} created successfully", report.Id);

        return Results.Created($"/reports/{report.Id}", reportDto);
    }

    private async Task<Guid?> allocateModerator(CancellationToken cancellationToken)
    {
        Guid? selectedModeratorId = null;

        // Find Moderator role id
         var moderatorRoleId = await _context.Roles
             .Where(r => r.Name == "Moderator")
             .Select(r => r.Id)
             .FirstOrDefaultAsync(cancellationToken);
        _logger.Information("Moderator role id resolved to: {RoleId}", moderatorRoleId);

         if (moderatorRoleId != Guid.Empty)
         {
             // Get moderator user ids
             var moderatorUserIds = await _context.UserRoles
                 .Where(ur => ur.RoleId == moderatorRoleId)
                 .Select(ur => ur.UserId)
                 .ToListAsync(cancellationToken);
            _logger.Information("Found {Count} moderator user ids", moderatorUserIds.Count);

             if (moderatorUserIds.Any())
             {
                 // For each moderator, compute the count of pending reports assigned to them (0 if none)
                 var moderatorWithCounts = await _context.Users
                     .Where(u => moderatorUserIds.Contains(u.Id))
                     .Select(u => new
                     {
                         u.Id,
                         PendingCount = _context.Reports.Count(r => r.Status == ReportStatus.PENDING && r.ModeratorId == u.Id)
                     })
                     .OrderBy(x => x.PendingCount)
                     .ThenBy(x => Guid.NewGuid()) // randomize ties
                     .FirstOrDefaultAsync(cancellationToken);

                 if (moderatorWithCounts != null)
                 {
                     selectedModeratorId = moderatorWithCounts.Id;
                 }
             }
         }

         // Fallback to random Admin if no moderators are available
         if (selectedModeratorId == null)
         {
             var adminRoleId = await _context.Roles
                 .Where(r => r.Name == "Admin")
                 .Select(r => r.Id)
                 .FirstOrDefaultAsync(cancellationToken);
            _logger.Information("Admin role id resolved to: {RoleId}", adminRoleId);

             if (adminRoleId != Guid.Empty)
             {
                 selectedModeratorId = await _context.UserRoles
                     .Where(ur => ur.RoleId == adminRoleId)
                     .Select(ur => ur.UserId)
                     .OrderBy(u => Guid.NewGuid())
                     .FirstOrDefaultAsync(cancellationToken);
                _logger.Information("Fallback selected admin id: {AdminId}", selectedModeratorId?.ToString() ?? "<none>");
             }
         }

         return selectedModeratorId;
    }
}
