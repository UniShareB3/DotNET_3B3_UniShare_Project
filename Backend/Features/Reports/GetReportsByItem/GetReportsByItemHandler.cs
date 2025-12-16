using AutoMapper;
using Backend.Features.Reports.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Reports.GetReportsByItem;

public class GetReportsByItemHandler : IRequestHandler<GetReportsByItemRequest, IResult>
{
    private readonly ApplicationContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger _logger = Log.ForContext<GetReportsByItemHandler>();

    public GetReportsByItemHandler(ApplicationContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IResult> Handle(GetReportsByItemRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving reports for item {ItemId}", request.ItemId);

        var reports = await _context.Reports
            .Where(r => r.ItemId == request.ItemId)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync(cancellationToken);

        var reportDtos = _mapper.Map<List<ReportDto>>(reports);
        _logger.Information("Retrieved {Count} reports for item {ItemId}", reportDtos.Count, request.ItemId);

        return Results.Ok(reportDtos);
    }
}
