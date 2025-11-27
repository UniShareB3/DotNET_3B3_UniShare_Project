using AutoMapper;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Backend.Features.Users.Dtos;

namespace Backend.Features.Users;

public class GetAllUsersHandler(ApplicationContext dbContext, IMapper mapper) : IRequestHandler<GetAllUsersRequest, IResult>
{
    public async Task<IResult> Handle(GetAllUsersRequest request, CancellationToken cancellationToken)
    {
        var users = await dbContext.Users
                .AsNoTracking()
                .Include(u => u.University)
                .Include(u => u.Items)
                .Select(u => mapper.Map<UserDto>(u))
                .ToListAsync(cancellationToken);

        return Results.Ok(users);
    }
}
