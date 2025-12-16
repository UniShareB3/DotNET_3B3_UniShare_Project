using AutoMapper;
using Backend.Data;
using Backend.Features.Users.Dtos;
using Backend.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Users.GetAdminAccount;

public class GetAdminAccountHandler : IRequestHandler<GetAdminAccountRequest, IResult>
{
    private readonly ApplicationContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger _logger = Log.ForContext<GetAdminAccountHandler>();
    private readonly IMapper _mapper;

    public GetAdminAccountHandler(ApplicationContext context, UserManager<User> userManager, IMapper mapper)
    {
        _context = context;
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<IResult> Handle(GetAdminAccountRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving admin account");

        var allUsers = await _context.Users.ToListAsync(cancellationToken);
        User? adminUser = null;
        
        foreach (var user in allUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                adminUser = user;
                break;
            }
        }

        if (adminUser == null)
        {
            _logger.Warning("No admin account found");
            return Results.NotFound(new { message = "No admin account found" });
        }

        var userDto = _mapper.Map<UserDto>(adminUser);

        _logger.Information("Retrieved admin account: {Email}", adminUser.Email);
        return Results.Ok(userDto);
    }
}
