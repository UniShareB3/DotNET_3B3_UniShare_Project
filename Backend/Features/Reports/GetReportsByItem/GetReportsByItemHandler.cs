using AutoMapper;
using Backend.Features.Reports.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Reports.GetReportsByItem;

public class GetReportsByItemHandler(ApplicationContext context, IMapper mapper)
    : IRequestHandler<GetReportsByItemRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GetReportsByItemHandler>();

    public async Task<IResult> Handle(GetReportsByItemRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving reports for item {ItemId}", request.ItemId);

        var reports = await context.Reports
            .Where(r => r.ItemId == request.ItemId)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync(cancellationToken);

        var reportDto = mapper.Map<List<ReportDto>>(reports);
        _logger.Information("Retrieved {Count} reports for item {ItemId}", reportDto.Count, request.ItemId);

        return Results.Ok(reportDto);
    }
}
