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
        
        // Selects the Moderator with the least number of PENDING reports assigned
        
        var moderatorId = await _context.Reports
            .Where(r => r.Status == ReportStatus.PENDING)
            .Select(r => r.ModeratorId)
            .GroupBy(id => id)
            .OrderBy(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefaultAsync(cancellationToken);

        if (moderatorId == null)
        {
            moderatorId = await _context.Users
                .Where(u => _context.UserRoles
                    .Any(ur => ur.UserId == u.Id && ur.RoleId == _context.Roles
                        .Where(r => r.Name == "Admin")
                        .Select(r => r.Id)
                        .FirstOrDefault()))
                .OrderBy(u => Guid.NewGuid())
                .Select(u => u.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
        
        report.ModeratorId = moderatorId;
        
        _context.Reports.Add(report);
        await _context.SaveChangesAsync(cancellationToken);

        var reportDto = _mapper.Map<ReportDto>(report);
        _logger.Information("Report {ReportId} created successfully", report.Id);

        return Results.Created($"/reports/{report.Id}", reportDto);
    }
}
