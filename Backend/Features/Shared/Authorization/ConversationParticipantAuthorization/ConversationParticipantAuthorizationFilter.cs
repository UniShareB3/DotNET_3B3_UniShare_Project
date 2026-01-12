using System.Security.Claims;
using Backend.Features.Conversations.DTO;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Shared.Authorization.ConversationParticipantAuthorization;

/// <summary>
/// Authorization filter that requires the user to be a participant in a conversation that contains the specified blob(s)
/// </summary>
public static class ConversationParticipantAuthorizationFilter
{
    /// <summary>
    /// Verifies that the current user is either the sender or receiver of a message containing the specified blob name(s)
    /// Works with both single and bulk document URL requests
    /// </summary>
    public static RouteHandlerBuilder RequireConversationParticipant(this RouteHandlerBuilder builder) 
        => builder.AddEndpointFilter(ProcessAuthorizationFilter);

    /// <summary>
    /// Verifies that the current user is a participant in a conversation containing the specified blob name(s) for route groups
    /// </summary>
    public static RouteGroupBuilder RequireConversationParticipant(this RouteGroupBuilder group) 
        => group.AddEndpointFilter(ProcessAuthorizationFilter);

    /// <summary>
    /// Filter delegate for authorization processing
    /// </summary>
    private static async ValueTask<object?> ProcessAuthorizationFilter(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await ProcessAuthorizationAsync(context, next);
        return result;
    }

    /// <summary>
    /// Processes the authorization check for conversation participant access
    /// </summary>
    private static async Task<object?> ProcessAuthorizationAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        if (httpContext.IsAdminBypassEnabled())
        {
            return await next(context);
        }

        var userId = ExtractUserIdFromClaims(httpContext);
        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var blobNames = ExtractBlobNames(context.Arguments);
        if (!HasValidBlobNames(blobNames))
        {
            return Results.BadRequest("At least one blob name is required");
        }

        var dbContext = httpContext.RequestServices.GetRequiredService<ApplicationContext>();
        return await AuthorizeUserAccessAsync(dbContext, userId, blobNames, next, context);
    }

    /// <summary>
    /// Extracts user ID from JWT claims
    /// </summary>
    private static Guid ExtractUserIdFromClaims(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? httpContext.User.FindFirstValue("sub");

        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Extracts blob names from request arguments
    /// </summary>
    private static List<string> ExtractBlobNames(IList<object?> arguments)
    {
        var blobNames = new List<string>();
        var argsList = arguments.ToList();
        CollectSingleBlobNames(argsList, blobNames);
        CollectBulkBlobNames(argsList, blobNames);
        return blobNames;
    }

    /// <summary>
    /// Collects blob names from single document URL requests
    /// </summary>
    private static void CollectSingleBlobNames(List<object?> arguments, List<string> blobNames)
    {
        var singleDtos = arguments.OfType<GetDocumentUrlDto>();
        foreach (var dto in singleDtos)
        {
            blobNames.Add(dto.BlobName);
        }
    }

    /// <summary>
    /// Collects blob names from bulk document URL requests
    /// </summary>
    private static void CollectBulkBlobNames(List<object?> arguments, List<string> blobNames)
    {
        var bulkDtos = arguments.OfType<GetBulkDocumentUrlsDto>();
        foreach (var bulkDto in bulkDtos)
        {
            blobNames.AddRange(bulkDto.BlobNames);
        }
    }

    /// <summary>
    /// Validates that blob names collection is not empty
    /// </summary>
    private static bool HasValidBlobNames(List<string> blobNames)
    {
        return blobNames.Count > 0;
    }

    /// <summary>
    /// Authorizes user access to the requested blobs
    /// </summary>
    private static async Task<object?> AuthorizeUserAccessAsync(
        ApplicationContext dbContext,
        Guid userId,
        List<string> blobNames,
        EndpointFilterDelegate next,
        EndpointFilterInvocationContext context)
    {
        var hasAccess = await VerifyUserAccessToBlobsAsync(dbContext, userId, blobNames);
        return hasAccess ? await next(context) : Results.Forbid();
    }

    /// <summary>
    /// Verifies that the user has access to all requested blobs
    /// </summary>
    private static async Task<bool> VerifyUserAccessToBlobsAsync(ApplicationContext dbContext, Guid currentUserId, List<string> blobNames)
    {
        foreach (var blobName in blobNames.Distinct())
        {
            var hasAccess = await CheckBlobAccessAsync(dbContext, blobName, currentUserId);
            if (!hasAccess)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if user has access to a specific blob
    /// </summary>
    private static async Task<bool> CheckBlobAccessAsync(ApplicationContext dbContext, string blobName, Guid currentUserId)
    {
        return await dbContext.ChatMessages
            .AnyAsync(m => m.BlobName != null 
                          && m.BlobName == blobName 
                          && (m.SenderId == currentUserId || m.ReceiverId == currentUserId));
    }
}
