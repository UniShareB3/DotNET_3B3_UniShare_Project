using AutoMapper;
using Backend.Features.Reports.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Reports.GetAllReports;

public class GetAllReportsHandler : IRequestHandler<GetAllReportsRequest, IResult>
{
    private readonly ApplicationContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger _logger = Log.ForContext<GetAllReportsHandler>();

    public GetAllReportsHandler(ApplicationContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IResult> Handle(GetAllReportsRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving all reports");

        var reports = await _context.Reports
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync(cancellationToken);

        var reportDtos = _mapper.Map<List<ReportDto>>(reports);
        _logger.Information("Retrieved {Count} reports", reportDtos.Count);

        return Results.Ok(reportDtos);
    }
}
