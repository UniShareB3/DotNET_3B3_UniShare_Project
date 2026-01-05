using AutoMapper;
using Backend.Features.ModeratorAssignment.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.ModeratorAssignment.GetAllModeratorAssignments;

public class GetAllModeratorAssignmentsHandler : IRequestHandler<GetAllModeratorAssignmentsRequest, IResult>
{
    private readonly ApplicationContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger _logger = Log.ForContext<GetAllModeratorAssignmentsHandler>();

    public GetAllModeratorAssignmentsHandler(ApplicationContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IResult> Handle(GetAllModeratorAssignmentsRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving all moderator assignments");

        var assignments = await _context.ModeratorAssignments
            .OrderByDescending(mr => mr.CreatedDate)
            .ToListAsync(cancellationToken);

        var assignmentDtos = _mapper.Map<List<ModeratorAssignmentDto>>(assignments);
        _logger.Information("Retrieved {Count} moderator assignments", assignmentDtos.Count);

        return Results.Ok(assignmentDtos);
    }
}
