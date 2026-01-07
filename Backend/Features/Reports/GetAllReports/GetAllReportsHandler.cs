using AutoMapper;
using Backend.Features.Reports.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Reports.GetAllReports;

public class GetAllReportsHandler(ApplicationContext context, IMapper mapper)
    : IRequestHandler<GetAllReportsRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GetAllReportsHandler>();

    public async Task<IResult> Handle(GetAllReportsRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving all reports");

        var reports = await context.Reports
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync(cancellationToken);

        var reportDto = mapper.Map<List<ReportDto>>(reports);
        _logger.Information("Retrieved {Count} reports", reportDto.Count);

        return Results.Ok(reportDto);
    }
}
