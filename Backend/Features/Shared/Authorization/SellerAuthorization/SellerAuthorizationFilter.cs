namespace Backend.Features.Shared.Authorization;

/// <summary>
/// Authorization filter that requires the user to have the Seller role
/// </summary>
public static class SellerAuthorizationFilter
{
    /// <summary>
    /// Verifies that the user is a seller (has successfully linked their Stripe account)
    /// </summary>
    public static RouteHandlerBuilder RequireSeller(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            
            var isSeller = httpContext.User.IsInRole("Seller");
            
            if (!isSeller)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Seller access required",
                    detail: "You must be a registered seller to access this resource. Please link your Stripe account first.");
            }

            return await next(context);
        });
    }

    /// <summary>
    /// Verifies that the user is a seller (for route groups)
    /// </summary>
    public static RouteGroupBuilder RequireSeller(this RouteGroupBuilder group)
    {
        return group.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            
            var isSeller = httpContext.User.IsInRole("Seller");
            
            if (!isSeller)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Seller access required",
                    detail: "You must be a registered seller to access this resource. Please link your Stripe account first.");
            }

            return await next(context);
        });
    }
}

