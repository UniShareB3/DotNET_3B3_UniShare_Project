using AutoMapper;
using Backend.Data;
using Backend.Features.Users.Dtos;
using Backend.Persistence;
using Microsoft.AspNetCore.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Users;

public class RegisterUserHandler(
    UserManager<User> userManager,
    IMediator mediator,
    IMapper mapper,
    ApplicationContext dbContext) : IRequestHandler<RegisterUserRequest, IResult>
{
    public async Task<IResult> Handle(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        RegisterUserDto registerUserDto = request.RegisterUserDto;
        var university = await dbContext.Universities
            .FirstOrDefaultAsync(u => u.Name == registerUserDto.UniversityName, cancellationToken);
        
        var user = mapper.Map<User>(registerUserDto);
        if (university != null) {
            user.UniversityId = university.Id;
        }

        var result = await userManager.CreateAsync(user, registerUserDto.Password);
        
        if (!result.Succeeded)
        {
            return Results.BadRequest(result.Errors);
        }
        
        await mediator.Send(new SendEmailVerificationRequest(user.Id), cancellationToken);
        
        var userDto = mapper.Map<UserDto>(user);
        
        return Results.Created($"/api/users/{user.Id}", new {
            message = "User registered successfully. Please verify your email.",
            entity = userDto
        });

    }
}