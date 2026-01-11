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
{
    // Delegate to the shared filter method
    return builder.AddEndpointFilter(VerifyParticipantAccess);
}

public static RouteGroupBuilder RequireConversationParticipant(this RouteGroupBuilder group)
{
    // Delegate to the shared filter method
    return group.AddEndpointFilter(VerifyParticipantAccess);
}

// ---------------------------------------------------------
// Shared Logic & Helpers
// ---------------------------------------------------------

private static async ValueTask<object?> VerifyParticipantAccess(
    EndpointFilterInvocationContext context, 
    EndpointFilterDelegate next)
{
    var httpContext = context.HttpContext;

    // 1. Bypass check
    if (httpContext.IsAdminBypassEnabled())
    {
        return await next(context);
    }

    // 2. User Authentication check
    if (!TryGetUserId(httpContext.User, out var currentUserId))
    {
        return Results.Unauthorized();
    }

    // 3. Extract required data
    var blobNames = ExtractBlobNames(context.Arguments);
    if (blobNames.Count == 0)
    {
        return Results.BadRequest("At least one blob name is required");
    }

    // 4. Verify Access
    var dbContext = httpContext.RequestServices.GetRequiredService<ApplicationContext>();
    if (!await UserHasAccessToBlobsAsync(dbContext, currentUserId, blobNames))
    {
        return Results.Forbid();
    }

    return await next(context);
}

private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) 
                      ?? user.FindFirstValue("sub");

    if (string.IsNullOrEmpty(userIdClaim))
    {
        userId = Guid.Empty;
        return false;
    }

    return Guid.TryParse(userIdClaim, out userId);
}

private static List<string> ExtractBlobNames(IList<object?> arguments)
{
    var blobNames = new List<string>();

    foreach (var arg in arguments)
    {
        if (arg is GetDocumentUrlDto singleDto)
        {
            blobNames.Add(singleDto.BlobName);
        }
        else if (arg is GetBulkDocumentUrlsDto bulkDto)
        {
            blobNames.AddRange(bulkDto.BlobNames);
        }
    }

    return blobNames;
}

private static async Task<bool> UserHasAccessToBlobsAsync(
    ApplicationContext dbContext, 
    Guid userId, 
    List<string> blobNames)
{
    foreach (var blobName in blobNames.Distinct())
    {
        var hasAccess = await dbContext.ChatMessages
            .AnyAsync(m => m.BlobName != null 
                           && m.BlobName == blobName 
                           && (m.SenderId == userId || m.ReceiverId == userId));

        if (!hasAccess)
        {
            return false;
        }
    }

    return true;
}
}
