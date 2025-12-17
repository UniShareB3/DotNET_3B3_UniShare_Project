using AutoMapper;
using Backend.Features.Reports.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Reports.GetReportsByModerator;

public class GetReportsByModeratorHandler : IRequestHandler<GetReportsByModeratorRequest, IResult>
{
    private readonly ApplicationContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger _logger = Log.ForContext<GetReportsByModeratorHandler>();

    public GetReportsByModeratorHandler(ApplicationContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IResult> Handle(GetReportsByModeratorRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving reports for moderator {ModeratorId}", request.ModeratorId);

        var reports = await _context.Reports
            .Where(r => r.ModeratorId == request.ModeratorId)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync(cancellationToken);

        var reportDtos = _mapper.Map<List<ReportDto>>(reports);
        _logger.Information("Retrieved {Count} reports for moderator {ModeratorId}", 
            reportDtos.Count, request.ModeratorId);

        return Results.Ok(reportDtos);
    }
}
