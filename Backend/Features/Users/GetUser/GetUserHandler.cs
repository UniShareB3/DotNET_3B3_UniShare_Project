using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Backend.Features.Users.DTO;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Users.GetUser;

public class GetUserHandler(ApplicationContext context, IMapper mapper) : IRequestHandler<GetUserRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GetUserHandler>();
    
    public async Task<IResult> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving user with ID: {UserId}", request.UserId);
        
        var user = await context.Users
            .Include(u => u.University)
            .Include(u => u.Items)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            _logger.Warning("User with ID {UserId} not found", request.UserId);
            return Results.NotFound();
        }

        var userDto = mapper.Map<UserDto>(user);

        _logger.Information("Successfully retrieved user {UserId}", request.UserId);
        return Results.Ok(userDto);
    }
}