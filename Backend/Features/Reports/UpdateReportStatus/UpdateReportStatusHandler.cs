using AutoMapper;
using Backend.Features.Reports.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Reports.UpdateReportStatus;

public class UpdateReportStatusHandler : IRequestHandler<UpdateReportStatusRequest, IResult>
{
    private readonly ApplicationContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger _logger = Log.ForContext<UpdateReportStatusHandler>();

    public UpdateReportStatusHandler(ApplicationContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IResult> Handle(UpdateReportStatusRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Updating status of report {ReportId} to {Status}", 
            request.ReportId, request.Dto.Status);

        var report = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

        if (report == null)
        {
            _logger.Warning("Report {ReportId} not found", request.ReportId);
            return Results.NotFound(new { message = "Report not found" });
        }

        // Verify moderator exists
        var moderator = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Dto.ModeratorId, cancellationToken);

        if (moderator == null)
        {
            _logger.Warning("Moderator {ModeratorId} not found", request.Dto.ModeratorId);
            return Results.NotFound(new { message = "Moderator not found" });
        }

        report.Status = request.Dto.Status;
        report.ModeratorId = request.Dto.ModeratorId;

        await _context.SaveChangesAsync(cancellationToken);

        var reportDto = _mapper.Map<ReportDto>(report);
        _logger.Information("Report {ReportId} status updated successfully to {Status}", 
            request.ReportId, request.Dto.Status);

        return Results.Ok(reportDto);
    }
}
