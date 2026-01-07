using AutoMapper;
using Backend.Features.Users.DTO;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Users.GetAllUsers;

public class GetAllUsersHandler(ApplicationContext dbContext, IMapper mapper) : IRequestHandler<GetAllUsersRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GetAllUsersHandler>();
    
    public async Task<IResult> Handle(GetAllUsersRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving all users from database");
        
        var users = await dbContext.Users
                .AsNoTracking()
                .Include(u => u.University)
                .Include(u => u.Items)
                .Select(u => mapper.Map<UserDto>(u))
                .ToListAsync(cancellationToken);

        _logger.Information("Retrieved {UserCount} users from database", users.Count);
        return Results.Ok(users);
    }
}
