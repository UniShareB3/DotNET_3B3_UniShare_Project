using System.Security.Claims;

namespace Backend.Features.Shared.Authorization;

/// <summary>
/// Authorization filter that requires the user's email to be verified
/// </summary>
public static class EmailVerificationFilter
{
    /// <summary>
    /// Verifies that the user's email is verified by checking the JWT token claim
    /// </summary>
    public static RouteHandlerBuilder RequireEmailVerification(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;

            // Check if admin bypass is enabled
            if (httpContext.IsAdminBypassEnabled())
            {
                return await next(context);
            }

            // Get email_verified claim from JWT token
            var emailVerifiedClaim = httpContext.User.FindFirstValue("email_verified");
            
            if (string.IsNullOrEmpty(emailVerifiedClaim))
                return Results.Unauthorized();

            // Check if email is verified (claim value is "true" or "false" as string)
            if (emailVerifiedClaim != "true")
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Email not verified",
                    detail: "Please verify your email address to access this resource");
            }

            return await next(context);
        });
    }

    /// <summary>
    /// Verifies that the user's email is verified by checking the JWT token claim (for route groups)
    /// </summary>
    public static RouteGroupBuilder RequireEmailVerification(this RouteGroupBuilder group)
    {
        return group.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;

            // Check if admin bypass is enabled
            if (httpContext.IsAdminBypassEnabled())
            {
                return await next(context);
            }

            // Get email_verified claim from JWT token
            var emailVerifiedClaim = httpContext.User.FindFirstValue("email_verified");
            
            if (string.IsNullOrEmpty(emailVerifiedClaim))
                return Results.Unauthorized();

            // Check if email is verified (claim value is "true" or "false" as string)
            if (emailVerifiedClaim != "true")
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Email not verified",
                    detail: "Please verify your email address to access this resource");
            }

            return await next(context);
        });
    }
}
