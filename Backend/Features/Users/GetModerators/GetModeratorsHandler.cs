using Backend.Data;
using Backend.Features.Users.DTO;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Users.GetModerators;

public class GetModeratorsHandler(UserManager<User> userManager) : IRequestHandler<GetModeratorsRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GetModeratorsHandler>();
    
    public async Task<IResult> Handle(GetModeratorsRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving all moderator users");
        
        var moderators = await userManager.GetUsersInRoleAsync("Moderator");
        var moderatorDto = moderators.Select(m => new UserRoleDto
        {
            Id = m.Id,
            Email = m.Email ?? string.Empty
        }).ToList();
        
        _logger.Information("Retrieved {ModeratorCount} moderator users", moderatorDto.Count);
        return Results.Ok(moderatorDto);
    }
}

