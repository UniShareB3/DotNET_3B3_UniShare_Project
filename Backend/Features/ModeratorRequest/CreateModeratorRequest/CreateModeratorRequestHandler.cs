using AutoMapper;
using Backend.Features.ModeratorRequest.DTO;
using Backend.Features.ModeratorRequest.Enums;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.ModeratorRequest.CreateModeratorRequest;

public class CreateModeratorRequestHandler : IRequestHandler<CreateModeratorRequestRequest, IResult>
{
    private readonly ApplicationContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger _logger = Log.ForContext<CreateModeratorRequestHandler>();

    public CreateModeratorRequestHandler(ApplicationContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IResult> Handle(CreateModeratorRequestRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Creating moderator request for user {UserId}", request.Dto.UserId);

        // Verify user exists
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Dto.UserId, cancellationToken);

        if (user == null)
        {
            _logger.Warning("User {UserId} not found", request.Dto.UserId);
            return Results.NotFound(new { message = "User not found" });
        }

        // Check if user already has a pending request
        var existingRequest = await _context.ModeratorRequests
            .FirstOrDefaultAsync(mr => mr.UserId == request.Dto.UserId 
                && mr.Status == ModeratorRequestStatus.PENDING, cancellationToken);

        if (existingRequest != null)
        {
            _logger.Warning("User {UserId} already has a pending moderator request", request.Dto.UserId);
            return Results.Conflict(new { message = "User already has a pending moderator request" });
        }
        

        var moderatorRequest = _mapper.Map<Data.ModeratorRequest>(request.Dto);
        moderatorRequest.CreatedDate = DateTime.UtcNow;
        moderatorRequest.Status = ModeratorRequestStatus.PENDING;

        _context.ModeratorRequests.Add(moderatorRequest);
        await _context.SaveChangesAsync(cancellationToken);

        var requestDto = _mapper.Map<ModeratorRequestDto>(moderatorRequest);
        _logger.Information("Moderator request {RequestId} created successfully for user {UserId}", 
            moderatorRequest.Id, request.Dto.UserId);

        return Results.Created($"/moderator-requests/{moderatorRequest.Id}", requestDto);
    }
}

