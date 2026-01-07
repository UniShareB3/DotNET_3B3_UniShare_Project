using AutoMapper;
using Backend.Features.ModeratorAssignment.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.ModeratorAssignment.GetAllModeratorAssignments;

public class GetAllModeratorAssignmentsHandler(ApplicationContext context, IMapper mapper)
    : IRequestHandler<GetAllModeratorAssignmentsRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GetAllModeratorAssignmentsHandler>();

    public async Task<IResult> Handle(GetAllModeratorAssignmentsRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving all moderator assignments");

        var assignments = await context.ModeratorAssignments
            .OrderByDescending(mr => mr.CreatedDate)
            .ToListAsync(cancellationToken);

        var assignmentDto = mapper.Map<List<ModeratorAssignmentDto>>(assignments);
        _logger.Information("Retrieved {Count} moderator assignments", assignmentDto.Count);

        return Results.Ok(assignmentDto);
    }
}
