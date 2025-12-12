using System.Security.Claims;

namespace Backend.Features.Shared.Authorization;

/// <summary>
/// Authorization filter that requires the user to be the owner of the resource
/// </summary>
public static class OwnerAuthorizationFilter
{
    /// <summary>
    /// Verifies that the userId in the URL matches the userId in the JWT token
    /// </summary>
    public static RouteHandlerBuilder RequireOwner(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;

            // Check if admin bypass is enabled
            if (httpContext.IsAdminBypassEnabled())
            {
                return await next(context);
            }
            
            // Get User ID from Token
            var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) 
                             ?? httpContext.User.FindFirstValue("sub");
            
            if (string.IsNullOrEmpty(userIdClaim))
                return Results.Unauthorized();

            // Get userId from route parameters
            var routeUserId = httpContext.Request.RouteValues["userId"]?.ToString();
            
            if (string.IsNullOrEmpty(routeUserId) || userIdClaim != routeUserId)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "You can only access your own resources");
            }

            return await next(context);
        });
    }

    /// <summary>
    /// Verifies that the userId in the URL matches the userId in the JWT token (for route groups)
    /// </summary>
    public static RouteGroupBuilder RequireOwner(this RouteGroupBuilder group)
    {
        return group.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;

            // Check if admin bypass is enabled
            if (httpContext.IsAdminBypassEnabled())
            {
                return await next(context);
            }
            
            var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) 
                             ?? httpContext.User.FindFirstValue("sub");
            
            if (string.IsNullOrEmpty(userIdClaim))
                return Results.Unauthorized();

            var routeUserId = httpContext.Request.RouteValues["userId"]?.ToString();
            
            if (string.IsNullOrEmpty(routeUserId) || userIdClaim != routeUserId)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "You can only access your own resources");
            }

            return await next(context);
        });
    }
}
