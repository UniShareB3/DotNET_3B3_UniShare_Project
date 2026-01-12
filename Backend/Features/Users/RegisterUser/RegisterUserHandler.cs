using AutoMapper;
using Backend.Data;
using Backend.Features.Shared.IAM.SendEmailVerification;
using Backend.Features.Users.DTO;
using Backend.Persistence;
using Microsoft.AspNetCore.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Users.RegisterUser;

public class RegisterUserHandler(
    UserManager<User> userManager,
    IMapper mapper,
    ApplicationContext dbContext) : IRequestHandler<RegisterUserRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<RegisterUserHandler>();

    public async Task<IResult> Handle(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        RegisterUserDto registerUserDto = request.RegisterUserDto;

        _logger.Information("Processing registration for email: {Email}", registerUserDto.Email);

        var university = await dbContext.Universities
            .FirstOrDefaultAsync(u => u.Name == registerUserDto.UniversityName, cancellationToken);

        if (university == null)
        {
            _logger.Warning("University {UniversityName} not found, user will be created without university",
                registerUserDto.UniversityName);
        }

        var user = mapper.Map<User>(registerUserDto);
        if (university != null)
        {
            user.UniversityId = university.Id;
        }

        var result = await userManager.CreateAsync(user, registerUserDto.Password);

        if (!result.Succeeded)
        {
            _logger.Error("User registration failed for {Email}. Errors: {Errors}",
                registerUserDto.Email,
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return Results.BadRequest(result.Errors);
        }

        // Assign default "User" role
        try
        {
            await userManager.AddToRoleAsync(user, "User");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to assign 'User' role to user {UserId}", user.Id);
        }

        var userDto = mapper.Map<UserDto>(user);

        _logger.Information("User {UserId} registered successfully with email {Email}",
            user.Id, registerUserDto.Email);

        return Results.Created($"/api/users/{user.Id}", new {
            message = "User registered successfully. Please verify your email.",
            entity = userDto
        });
    }
}