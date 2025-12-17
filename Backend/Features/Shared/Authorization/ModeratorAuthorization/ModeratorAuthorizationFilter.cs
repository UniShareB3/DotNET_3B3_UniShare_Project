namespace Backend.Features.Shared.Authorization;

/// <summary>
/// Authorization filter that requires the user to have Admin or Moderator role
/// </summary>
public static class ModeratorAuthorizationFilter
{
    /// <summary>
    /// Verifies that the user is an admin or moderator
    /// </summary>
    public static RouteHandlerBuilder RequireAdminOrModerator(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;

            var isAdmin = httpContext.User.IsInRole("Admin");
            var isModerator = httpContext.User.IsInRole("Moderator");

            if (!isAdmin && !isModerator)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Admin or Moderator access required",
                    detail: "You must be an admin or moderator to access this resource");
            }

            return await next(context);
        });
    }

    /// <summary>
    /// Verifies that the user is an admin or moderator (for route groups)
    /// </summary>
    public static RouteGroupBuilder RequireAdminOrModerator(this RouteGroupBuilder group)
    {
        return group.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;

            var isAdmin = httpContext.User.IsInRole("Admin");
            var isModerator = httpContext.User.IsInRole("Moderator");

            if (!isAdmin && !isModerator)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Admin or Moderator access required",
                    detail: "You must be an admin or moderator to access this resource");
            }

            return await next(context);
        });
    }
}

