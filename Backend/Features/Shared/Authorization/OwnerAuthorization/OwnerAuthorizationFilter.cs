using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Backend.Features.Shared.Authorization;

/// <summary>
/// Authorization filter that requires the user to be the owner of the resource
/// </summary>
public static class OwnerAuthorizationFilter
{
    /// <summary>
    /// Verifies that the userId in the URL or request body matches the userId in the JWT token
    /// </summary>
    public static RouteHandlerBuilder RequireOwner(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            var logger = httpContext.RequestServices.GetService<ILogger<Microsoft.AspNetCore.Http.HttpContext>>();

            // 1. Admin Bypass Check
            if (httpContext.IsAdminBypassEnabled())
            {
                logger?.LogInformation("[RequireOwner] Admin bypass enabled, allowing access");
                return await next(context);
            }

            // 2. Identify the Authenticated User
            var authUserId = GetAuthenticatedUserId(httpContext.User);
            if (string.IsNullOrEmpty(authUserId))
            {
                logger?.LogWarning("[RequireOwner] No user ID claim found in token");
                return Results.Unauthorized();
            }

            // 3. Identify the Resource Owner (Target User)
            // We moved the complex reflection/route logic into this helper
            var targetUserId = GetTargetResourceId(context, logger);

            // 4. Compare and Authorize
            if (string.IsNullOrEmpty(targetUserId) || authUserId != targetUserId)
            {
                logger?.LogWarning("[RequireOwner] Access denied. Auth User: {AuthId}, Target Resource: {TargetId}",
                    authUserId, targetUserId ?? "Not Found");

                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "You can only access your own resources");
            }

            logger?.LogInformation("[RequireOwner] Authorization successful");
            return await next(context);
        });
    }

    /// <summary>
    /// Verifies that the userId in the URL or request body matches the userId in the JWT token (for route groups)
    /// </summary>
    public static RouteGroupBuilder RequireOwner(this RouteGroupBuilder group)
    {
        return group.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            var logger = httpContext.RequestServices.GetService<ILogger<Microsoft.AspNetCore.Http.HttpContext>>();

            // 1. Admin Bypass
            if (httpContext.IsAdminBypassEnabled())
            {
                logger?.LogInformation("[RequireOwner] Admin bypass enabled, allowing access");
                return await next(context);
            }

            // 2. Identify Authenticated User
            var authUserId = GetAuthenticatedUserId(httpContext.User);
            if (string.IsNullOrEmpty(authUserId))
            {
                logger?.LogWarning("[RequireOwner] No user ID claim found in token");
                return Results.Unauthorized();
            }

            logger?.LogInformation("[RequireOwner] User ID from token: {UserId}", authUserId);

            // 3. Identify Target Resource Owner (Hidden complexity here)
            var targetUserId = GetTargetResourceId(context, logger);

            // 4. Validate and Authorize
            if (string.IsNullOrEmpty(targetUserId) || authUserId != targetUserId)
            {
                logger?.LogWarning("[RequireOwner] Access denied. Auth User: {AuthId}, Target: {TargetId}",
                    authUserId, targetUserId ?? "Not Found");

                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "You can only access your own resources");
            }

            logger?.LogInformation("[RequireOwner] Authorization successful");
            return await next(context);
        });
    }
    
    // --- Helper Methods ---

    private static string? GetAuthenticatedUserId(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? user.FindFirstValue("sub");
    }

    private static string? GetTargetResourceId(EndpointFilterInvocationContext context, ILogger? logger)
    {
        // Priority 1: Check Route Parameters
        if (context.HttpContext.Request.RouteValues.TryGetValue("userId", out var routeVal) && routeVal != null)
        {
            var val = routeVal.ToString();
            logger?.LogInformation("[RequireOwner] Found userId in route: {UserId}", val);
            return val;
        }

        logger?.LogInformation("[RequireOwner] No userId in route, checking request body");

        // Priority 2: Check Body/Arguments via Reflection
        foreach (var arg in context.Arguments)
        {
            if (arg == null) continue;

            var prop = arg.GetType().GetProperty("UserId");
            if (prop != null)
            {
                var val = prop.GetValue(arg)?.ToString();
                if (!string.IsNullOrEmpty(val))
                {
                    logger?.LogInformation("[RequireOwner] Found UserId in request body: {UserId}", val);
                    return val;
                }
            }
        }

        return null;
    }
}