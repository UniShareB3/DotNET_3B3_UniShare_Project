namespace Backend.Features.Shared.Authorization;

/// <summary>
/// Provides extension methods for adding an admin bypass filter to endpoints.
/// </summary>
public static class AdminBypassFilter
{
    private const string AdminBypassKey = "__AdminBypassEnabled";

    /// <summary>
    /// Adds a filter that allows admins to bypass other authorization checks.
    /// This should be registered before other authorization filters.
    /// </summary>
    public static RouteHandlerBuilder AllowAdmin(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            if (httpContext.User.IsInRole("Admin"))
            {
                httpContext.Items[AdminBypassKey] = true;
            }
            return await next(context);
        });
    }

    /// <summary>
    /// Adds a filter that allows admins to bypass other authorization checks for a route group.
    /// This should be registered before other authorization filters.
    /// </summary>
    public static RouteGroupBuilder AllowAdmin(this RouteGroupBuilder group)
    {
        return group.AddEndpointFilter(async (context, next) =>
        {
            Console.WriteLine("HERE2");
            var httpContext = context.HttpContext;
            if (httpContext.User.IsInRole("Admin"))
            {
                httpContext.Items[AdminBypassKey] = true;
            }
            return await next(context);
        });
    }

    /// <summary>
    /// Checks if the admin bypass is enabled for the current request.
    /// </summary>
    public static bool IsAdminBypassEnabled(this HttpContext httpContext)
    {
        return httpContext.Items.ContainsKey(AdminBypassKey);
    }
}
