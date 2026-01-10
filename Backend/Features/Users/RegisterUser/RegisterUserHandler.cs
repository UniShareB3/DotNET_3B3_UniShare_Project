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
    IMediator mediator,
    IMapper mapper,
    ApplicationContext dbContext) : IRequestHandler<RegisterUserRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<RegisterUserHandler>();

    public async Task<IResult> Handle(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        RegisterUserDto registerUserDto = request.RegisterUserDto;
        
        _logger.Information("Attempting to register user with email: {Email}", registerUserDto.Email);
        
        var university = await dbContext.Universities
            .FirstOrDefaultAsync(u => u.Name == registerUserDto.UniversityName, cancellationToken);
        
        if (university != null)
        {
            _logger.Information("Found university {UniversityName} with ID {UniversityId}", 
                university.Name, university.Id);
        }
        else
        {
            _logger.Warning("University {UniversityName} not found, user will be created without university", 
                registerUserDto.UniversityName);
        }
        
        var user = mapper.Map<User>(registerUserDto);
        if (university != null) {
            user.UniversityId = university.Id;
        }

        _logger.Information("Creating user account for {Email}", registerUserDto.Email);
        
        var result = await userManager.CreateAsync(user, registerUserDto.Password);
        
        if (!result.Succeeded)
        {
            _logger.Error("User registration failed for {Email}. Errors: {Errors}", 
                registerUserDto.Email, 
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return Results.BadRequest(result.Errors);
        }
        
        _logger.Information("User {UserId} created successfully", user.Id);
        
        // Assign default "User" role
        try
        {
            await userManager.AddToRoleAsync(user, "User");
            _logger.Information("Assigned 'User' role to user {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to assign 'User' role to user {UserId}", user.Id);
        }
        
        _logger.Information("Sending email verification request for user {UserId}", user.Id);
        
        var userDto = mapper.Map<UserDto>(user);
        
        _logger.Information("User registration completed successfully for {UserId}", user.Id);
        
        return Results.Created($"/api/users/{user.Id}", new {
            message = "User registered successfully. Please verify your email.",
            entity = userDto
        });

    }
}