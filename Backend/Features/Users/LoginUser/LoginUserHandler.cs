using Microsoft.AspNetCore.Identity;
using Backend.Persistence;
using Backend.Data;
using Backend.Features.Users.DTO;
using Backend.Services.Token;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Users.LoginUser;

public class LoginUserHandler(
    UserManager<User> userManager, 
    ITokenService tokenService,
    ApplicationContext context) : IRequestHandler<LoginUserRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<LoginUserHandler>();

    public async Task<IResult> Handle(LoginUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            _logger.Warning("Login failed: User not found for email {Email}", request.Email);
            return Results.Unauthorized();
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            _logger.Warning("Login failed: Invalid password for user {UserId}", user.Id);
            return Results.Unauthorized();
        }

        var existingTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == user.Id)
            .ToListAsync(cancellationToken);

        if (existingTokens.Count != 0)
        {
            context.RefreshTokens.RemoveRange(existingTokens);
        }

        // Get user roles
        var roles = await userManager.GetRolesAsync(user);

        var accessToken = tokenService.GenerateToken(user, roles);
        var refreshTokenString = tokenService.GenerateRefreshToken();

        var tokenFamily = Guid.NewGuid();

        var refreshToken = new RefreshToken
        {
            Token = refreshTokenString,
            UserId = user.Id,
            ExpiresAt = tokenService.GetRefreshTokenExpirationDate(),
            TokenFamily = tokenFamily,
            ParentTokenId = null,
            ReplacedByTokenId = null
        };

        context.RefreshTokens.Add(refreshToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error saving refresh token for user {UserId}", user.Id);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while processing your request.");
        }

        var response = new LoginUserResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshTokenString,
            ExpiresIn: tokenService.GetAccessTokenExpirationInSeconds()
        );

        _logger.Information("Login successful for user {UserId} with roles: {Roles}",
            user.Id, string.Join(", ", roles));

        return Results.Ok(response);
    }
}