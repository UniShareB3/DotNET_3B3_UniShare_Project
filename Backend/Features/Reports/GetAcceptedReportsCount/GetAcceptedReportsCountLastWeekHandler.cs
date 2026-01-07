using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;
using Backend.Features.Reports.Enums;

namespace Backend.Features.Reports.GetAcceptedReportsCount;

public class GetAcceptedReportsCountLastWeekHandler(ApplicationContext context)
    : IRequestHandler<GetAcceptedReportsCountLastWeekRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GetAcceptedReportsCountLastWeekHandler>();

    public async Task<IResult> Handle(GetAcceptedReportsCountLastWeekRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving accepted reports count for last week for item {ItemId}", request.ItemId);
        
        var period = DateTime.UtcNow.AddDays(-request.NumberOfDays);

        var count = await context.Reports
            .Where(r => r.ItemId == request.ItemId 
                        && r.Status == ReportStatus.Accepted
                        && r.CreatedDate >= period)
            .CountAsync(cancellationToken);

        _logger.Information("Item {ItemId} has {Count} accepted reports in the last week", request.ItemId, count);

        return Results.Ok(new { itemId = request.ItemId, count });
    }
}
