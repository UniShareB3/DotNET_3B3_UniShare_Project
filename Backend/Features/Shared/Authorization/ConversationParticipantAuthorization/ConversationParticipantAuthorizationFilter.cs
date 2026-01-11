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

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
            {
                return Results.Unauthorized();
            }

            // Get database context
            var dbContext = httpContext.RequestServices.GetRequiredService<ApplicationContext>();

            // Extract blob names from request arguments
            var blobNames = new List<string>();
            
            foreach (var arg in context.Arguments)
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

            if (blobNames.Count == 0)
            {
                return Results.BadRequest("At least one blob name is required");
            }

            // Check if the user is a participant in conversations containing ALL requested blobs
            foreach (var blobName in blobNames.Distinct())
            {
                var hasAccess = await dbContext.ChatMessages
                    .AnyAsync(m => m.BlobName != null 
                                  && m.BlobName == blobName 
                                  && (m.SenderId == currentUserId || m.ReceiverId == currentUserId));

                if (!hasAccess)
                {
                    return Results.Forbid();
                }
            }

            return await next(context);
        });
    }

    /// <summary>
    /// Verifies that the current user is a participant in a conversation containing the specified blob name(s) for route groups
    /// </summary>
    public static RouteGroupBuilder RequireConversationParticipant(this RouteGroupBuilder group)
    {
        return group.AddEndpointFilter(async (context, next) =>
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

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
            {
                return Results.Unauthorized();
            }

            // Get database context
            var dbContext = httpContext.RequestServices.GetRequiredService<ApplicationContext>();

            // Extract blob names from request arguments
            var blobNames = new List<string>();
            
            foreach (var arg in context.Arguments)
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

            if (blobNames.Count == 0)
            {
                return Results.BadRequest("At least one blob name is required");
            }

            // Check if the user is a participant in conversations containing ALL requested blobs
            foreach (var blobName in blobNames.Distinct())
            {
                var hasAccess = await dbContext.ChatMessages
                    .AnyAsync(m => m.BlobName != null 
                                  && m.BlobName == blobName 
                                  && (m.SenderId == currentUserId || m.ReceiverId == currentUserId));

                if (!hasAccess)
                {
                    return Results.Forbid();
                }
            }

            return await next(context);
        });
    }
}
