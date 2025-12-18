using AutoMapper;
using Backend.Features.ModeratorAssignment.DTO;
using Backend.Features.ModeratorAssignment.Enums;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.ModeratorAssignment.CreateModeratorAssignment;

public class CreateModeratorAssignmentHandler : IRequestHandler<CreateModeratorAssignmentRequest, IResult>
{
    private readonly ApplicationContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger _logger = Log.ForContext<CreateModeratorAssignmentHandler>();

    public CreateModeratorAssignmentHandler(ApplicationContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IResult> Handle(CreateModeratorAssignmentRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Creating moderator assignment for user {UserId}", request.Dto.UserId);

        // Verify user exists
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Dto.UserId, cancellationToken);

        if (user == null)
        {
            _logger.Warning("User {UserId} not found", request.Dto.UserId);
            return Results.NotFound(new { message = "User not found" });
        }

        // Check if user already has a pending assignment
        var existingAssignment = await _context.ModeratorAssignments
            .FirstOrDefaultAsync(mr => mr.UserId == request.Dto.UserId 
                && mr.Status == ModeratorAssignmentStatus.PENDING, cancellationToken);

        if (existingAssignment != null)
        {
            _logger.Warning("User {UserId} already has a pending moderator assignment", request.Dto.UserId);
            return Results.Conflict(new { message = "User already has a pending moderator assignment" });
        }

        var moderatorAssignment = _mapper.Map<Data.ModeratorAssignment>(request.Dto);
        moderatorAssignment.CreatedDate = DateTime.UtcNow;
        moderatorAssignment.Status = ModeratorAssignmentStatus.PENDING;

        _context.ModeratorAssignments.Add(moderatorAssignment);
        await _context.SaveChangesAsync(cancellationToken);

        var assignmentDto = _mapper.Map<ModeratorAssignmentDto>(moderatorAssignment);
        _logger.Information("Moderator assignment {AssignmentId} created successfully for user {UserId}", 
            moderatorAssignment.Id, request.Dto.UserId);

        return Results.Created($"/moderator-assignments/{moderatorAssignment.Id}", assignmentDto);
    }
}
