using AutoMapper;
using Backend.Features.Reports.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Reports.GetReportsByModerator;

public class GetReportsByModeratorHandler(ApplicationContext context, IMapper mapper)
    : IRequestHandler<GetReportsByModeratorRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GetReportsByModeratorHandler>();

    public async Task<IResult> Handle(GetReportsByModeratorRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving reports for moderator {ModeratorId}", request.ModeratorId);

        var reports = await context.Reports
            .Where(r => r.ModeratorId == request.ModeratorId)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync(cancellationToken);

        var reportDto = mapper.Map<List<ReportDto>>(reports);
        _logger.Information("Retrieved {Count} reports for moderator {ModeratorId}", 
            reportDto.Count, request.ModeratorId);

        return Results.Ok(reportDto);
    }
}
