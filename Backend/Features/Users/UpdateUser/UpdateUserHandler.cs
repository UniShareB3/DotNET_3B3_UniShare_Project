using AutoMapper;
using Backend.Data;
using Backend.Persistence;
using Microsoft.AspNetCore.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Users;

public class UpdateUserHandler(
    UserManager<User> userManager,
    ApplicationContext dbContext,
    IMapper mapper) : IRequestHandler<UpdateUserRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<UpdateUserHandler>();

    public async Task<IResult> Handle(UpdateUserRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to update user {UserId}", request.UserId);

        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        
        if (user == null)
        {
            _logger.Warning("Update failed: User {UserId} not found.", request.UserId);
            return Results.NotFound(new { error = "User not found" });
        }

        var dto = request.UpdateUserDto;

        // Update FirstName if provided
        if (!string.IsNullOrWhiteSpace(dto.FirstName))
        {
            user.FirstName = dto.FirstName;
            _logger.Information("Updated FirstName for user {UserId}", request.UserId);
        }

        // Update LastName if provided
        if (!string.IsNullOrWhiteSpace(dto.LastName))
        {
            user.LastName = dto.LastName;
            _logger.Information("Updated LastName for user {UserId}", request.UserId);
        }

        // Update Email if provided
        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
        {
            user.Email = dto.Email;
            user.NormalizedEmail = dto.Email.ToUpper();
            user.UserName = dto.Email;
            user.NormalizedUserName = dto.Email.ToUpper();
            user.NewEmailConfirmed = false; // Reset email confirmation
            _logger.Information("Updated Email for user {UserId}, email confirmation reset", request.UserId);
        }

        // Update University if provided
        if (!string.IsNullOrWhiteSpace(dto.UniversityName))
        {
            var university = await dbContext.Universities
                .FirstOrDefaultAsync(u => u.Name == dto.UniversityName, cancellationToken);
            
            if (university != null)
            {
                user.UniversityId = university.Id;
                _logger.Information("Updated University to {UniversityName} for user {UserId}", dto.UniversityName, request.UserId);
            }
            else
            {
                _logger.Warning("University {UniversityName} not found for user {UserId}", dto.UniversityName, request.UserId);
                return Results.BadRequest(new { error = $"University '{dto.UniversityName}' not found" });
            }
        }

        // Update using UserManager to trigger validators
        var result = await userManager.UpdateAsync(user);
        
        if (!result.Succeeded)
        {
            _logger.Error("User update failed for {UserId}. Errors: {Errors}", 
                request.UserId, 
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return Results.BadRequest(result.Errors);
        }

        // Update Password if provided (separately to trigger password validators)
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await userManager.ResetPasswordAsync(user, token, dto.Password);
            
            if (!passwordResult.Succeeded)
            {
                _logger.Error("Password update failed for {UserId}. Errors: {Errors}", 
                    request.UserId, 
                    string.Join(", ", passwordResult.Errors.Select(e => e.Description)));
                return Results.BadRequest(passwordResult.Errors);
            }
            
            _logger.Information("Updated Password for user {UserId}", request.UserId);
        }

        var userDto = mapper.Map<Dtos.UserDto>(user);
        
        _logger.Information("User {UserId} updated successfully", request.UserId);
        
        return Results.Ok(new {
            message = "User updated successfully",
            entity = userDto
        });
    }
}
