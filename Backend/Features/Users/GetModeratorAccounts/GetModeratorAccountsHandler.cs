using AutoMapper;
using Backend.Data;
using Backend.Features.Users.Dtos;
using Backend.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Users.GetModeratorAccounts;

public class GetModeratorAccountsHandler : IRequestHandler<GetModeratorAccountsRequest, IResult>
{
    private readonly ApplicationContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;
    private readonly ILogger _logger = Log.ForContext<GetModeratorAccountsHandler>();

    public GetModeratorAccountsHandler(ApplicationContext context, UserManager<User> userManager, IMapper mapper)
    {
        _context = context;
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<IResult> Handle(GetModeratorAccountsRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving all moderator accounts");

        var allUsers = await _context.Users.ToListAsync(cancellationToken);
        var moderatorUsers = new List<User>();
        
        foreach (var user in allUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Moderator"))
            {
                moderatorUsers.Add(user);
            }
        }

        var moderatorDtos = _mapper.Map<List<UserDto>>(moderatorUsers);

        _logger.Information("Retrieved {Count} moderator accounts", moderatorDtos.Count);
        return Results.Ok(moderatorDtos);
    }
}
