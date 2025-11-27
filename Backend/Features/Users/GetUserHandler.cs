using Backend.Features.Users.Dtos;
using Backend.Persistence;
using Microsoft.AspNetCore.Identity;
using Backend.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace Backend.Features.Users;

public class GetUserHandler(ApplicationContext context, IMapper mapper) : IRequestHandler<GetUserRequest, IResult>
{
    public async Task<IResult> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .Include(u => u.University)
            .Include(u => u.Items)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            return Results.NotFound();
        }

        var userDto = mapper.Map<UserDto>(user);

        return Results.Ok(userDto);
    }
}