using Backend.Data;
using Backend.Features.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Users.GetAdmins;

public class GetAdminsHandler(UserManager<User> userManager) : IRequestHandler<GetAdminsRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GetAdminsHandler>();
    
    public async Task<IResult> Handle(GetAdminsRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving all admin users");
        
        var admins = await userManager.GetUsersInRoleAsync("Admin");
        var adminDtos = admins.Select(a => new UserRoleDto
        {
            Id = a.Id,
            Email = a.Email ?? string.Empty
        }).ToList();
        
        _logger.Information("Retrieved {AdminCount} admin users", adminDtos.Count);
        return Results.Ok(adminDtos);
    }
}

