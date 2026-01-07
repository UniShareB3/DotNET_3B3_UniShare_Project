using Backend.Data;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;
using Backend.Features.Reports.Enums;

namespace Backend.Features.Reports.GetAcceptedReportsCount;

public class GetAcceptedReportsCountLastWeekHandler : IRequestHandler<GetAcceptedReportsCountLastWeekRequest, IResult>
{
    private readonly ApplicationContext _context;
    private readonly ILogger _logger = Log.ForContext<GetAcceptedReportsCountLastWeekHandler>();

    public GetAcceptedReportsCountLastWeekHandler(ApplicationContext context)
    {
        _context = context;
    }

    public async Task<IResult> Handle(GetAcceptedReportsCountLastWeekRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving accepted reports count for last week for item {ItemId}", request.ItemId);
        
        var period = DateTime.UtcNow.AddDays(-request.NumberOfDays);

        var count = await _context.Reports
            .Where(r => r.ItemId == request.ItemId 
                        && r.Status == ReportStatus.ACCEPTED 
                        && r.CreatedDate >= period)
            .CountAsync(cancellationToken);

        _logger.Information("Item {ItemId} has {Count} accepted reports in the last week", request.ItemId, count);

        return Results.Ok(new { itemId = request.ItemId, count });
    }
}
