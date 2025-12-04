

namespace Backend.Features.Shared.Authorization;

/// <summary>
/// Authorization filter that requires the user to have the Admin role
/// </summary>
public static class AdminAuthorizationFilter
{
    /// <summary>
    /// Verifies that the user is an admin
    /// </summary>
    public static RouteHandlerBuilder RequireAdmin(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            
            var isAdmin = httpContext.User.IsInRole("Admin");
            
            if (!isAdmin)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Admin access required",
                    detail: "You must be an admin to access this resource");
            }

            return await next(context);
        });
    }

    /// <summary>
    /// Verifies that the user is an admin (for route groups)
    /// </summary>
    public static RouteGroupBuilder RequireAdmin(this RouteGroupBuilder group)
    {
        return group.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            
            var isAdmin = httpContext.User.IsInRole("Admin");
            
            if (!isAdmin)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Admin access required",
                    detail: "You must be an admin to access this resource");
            }

            return await next(context);
        });
    }
}

