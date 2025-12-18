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

            // Check if admin bypass is enabled
            if (httpContext.IsAdminBypassEnabled())
            {
                logger?.LogInformation("[RequireOwner] Admin bypass enabled, allowing access");
                return await next(context);
            }
            
            // Get User ID from Token
            var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) 
                             ?? httpContext.User.FindFirstValue("sub");
            
            if (string.IsNullOrEmpty(userIdClaim))
            {
                logger?.LogWarning("[RequireOwner] No user ID claim found in token");
                return Results.Unauthorized();
            }

            logger?.LogInformation("[RequireOwner] User ID from token: {UserId}", userIdClaim);

            // Get userId from route parameters first
            var routeUserId = httpContext.Request.RouteValues["userId"]?.ToString();
            
            // If not in route, try to get UserId from request body/arguments
            if (string.IsNullOrEmpty(routeUserId))
            {
                logger?.LogInformation("[RequireOwner] No userId in route, checking request body");
                
                // Check endpoint arguments for UserId property
                foreach (var arg in context.Arguments)
                {
                    if (arg != null)
                    {
                        var userIdProperty = arg.GetType().GetProperty("UserId");
                        if (userIdProperty != null)
                        {
                            var userIdValue = userIdProperty.GetValue(arg);
                            if (userIdValue != null)
                            {
                                routeUserId = userIdValue.ToString();
                                logger?.LogInformation("[RequireOwner] Found UserId in request body: {UserId}", routeUserId);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                logger?.LogInformation("[RequireOwner] Found userId in route: {UserId}", routeUserId);
            }
            
            if (string.IsNullOrEmpty(routeUserId))
            {
                logger?.LogWarning("[RequireOwner] No userId found in route or request body");
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "You can only access your own resources");
            }
            
            if (userIdClaim != routeUserId)
            {
                logger?.LogWarning("[RequireOwner] User ID mismatch - Token: {TokenUserId}, Resource: {ResourceUserId}", 
                    userIdClaim, routeUserId);
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

            // Check if admin bypass is enabled
            if (httpContext.IsAdminBypassEnabled())
            {
                logger?.LogInformation("[RequireOwner] Admin bypass enabled, allowing access");
                return await next(context);
            }
            
            var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) 
                             ?? httpContext.User.FindFirstValue("sub");
            
            if (string.IsNullOrEmpty(userIdClaim))
            {
                logger?.LogWarning("[RequireOwner] No user ID claim found in token");
                return Results.Unauthorized();
            }

            logger?.LogInformation("[RequireOwner] User ID from token: {UserId}", userIdClaim);

            // Get userId from route parameters first
            var routeUserId = httpContext.Request.RouteValues["userId"]?.ToString();
            
            // If not in route, try to get UserId from request body/arguments
            if (string.IsNullOrEmpty(routeUserId))
            {
                logger?.LogInformation("[RequireOwner] No userId in route, checking request body");
                
                // Check endpoint arguments for UserId property
                foreach (var arg in context.Arguments)
                {
                    if (arg != null)
                    {
                        var userIdProperty = arg.GetType().GetProperty("UserId");
                        if (userIdProperty != null)
                        {
                            var userIdValue = userIdProperty.GetValue(arg);
                            if (userIdValue != null)
                            {
                                routeUserId = userIdValue.ToString();
                                logger?.LogInformation("[RequireOwner] Found UserId in request body: {UserId}", routeUserId);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                logger?.LogInformation("[RequireOwner] Found userId in route: {UserId}", routeUserId);
            }
            
            if (string.IsNullOrEmpty(routeUserId))
            {
                logger?.LogWarning("[RequireOwner] No userId found in route or request body");
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "You can only access your own resources");
            }
            
            if (userIdClaim != routeUserId)
            {
                logger?.LogWarning("[RequireOwner] User ID mismatch - Token: {TokenUserId}, Resource: {ResourceUserId}", 
                    userIdClaim, routeUserId);
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "You can only access your own resources");
            }

            logger?.LogInformation("[RequireOwner] Authorization successful");
            return await next(context);
        });
    }
}
