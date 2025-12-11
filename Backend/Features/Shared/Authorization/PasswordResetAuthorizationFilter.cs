using System.Security.Claims;

namespace Backend.Features.Shared.Authorization;

/// <summary>
/// Authorization filter that validates password reset token or allows admin access
/// </summary>
public static class PasswordResetAuthorizationFilter
{
    /// <summary>
    /// Verifies that the user has a valid password reset token or is an admin
    /// </summary>
    public static RouteHandlerBuilder RequirePasswordResetToken(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;

            // Check if admin bypass is enabled
            if (httpContext.IsAdminBypassEnabled())
            {
                return await next(context);
            }

            // Check for password_reset claim in the token
            var passwordResetClaim = httpContext.User.FindFirstValue("password_reset");
            
            if (passwordResetClaim != "true")
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Invalid token",
                    detail: "A valid password reset token is required to change the password");
            }

            return await next(context);
        });
    }
}

