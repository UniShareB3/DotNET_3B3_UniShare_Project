using AutoMapper;
using Backend.Features.ModeratorRequest.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.ModeratorRequest.GetAllModeratorRequests;

public class GetAllModeratorRequestsHandler : IRequestHandler<GetAllModeratorRequestsRequest, IResult>
{
    private readonly ApplicationContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger _logger = Log.ForContext<GetAllModeratorRequestsHandler>();

    public GetAllModeratorRequestsHandler(ApplicationContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IResult> Handle(GetAllModeratorRequestsRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving all moderator requests");

        var requests = await _context.ModeratorRequests
            .OrderByDescending(mr => mr.CreatedDate)
            .ToListAsync(cancellationToken);

        var requestDtos = _mapper.Map<List<ModeratorRequestDto>>(requests);
        _logger.Information("Retrieved {Count} moderator requests", requestDtos.Count);

        return Results.Ok(requestDtos);
    }
}
