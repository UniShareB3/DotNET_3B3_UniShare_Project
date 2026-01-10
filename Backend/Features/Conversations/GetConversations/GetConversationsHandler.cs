using Backend.Features.Conversations.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Conversations.GetConversations;

public class GetConversationsHandler(ApplicationContext dbContext) : IRequestHandler<GetConversationsRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GetConversationsHandler>();

    public async Task<IResult> Handle(GetConversationsRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving conversations for user {UserId}", request.CurrentUserId);

        try
        {
            // Get all unique user IDs we've chatted with
            var sentToUserIds = dbContext.ChatMessages
                .Where(m => m.SenderId == request.CurrentUserId)
                .Select(m => m.ReceiverId);

            var receivedFromUserIds = dbContext.ChatMessages
                .Where(m => m.ReceiverId == request.CurrentUserId)
                .Select(m => m.SenderId);

            var allUserIds = await sentToUserIds.Union(receivedFromUserIds)
                .Distinct()
                .ToListAsync(cancellationToken);

            // Get user details and last message for each conversation
            var conversations = new List<ConversationDto>();
            foreach (var otherUserId in allUserIds)
            {
                var otherUser = await dbContext.Users.FindAsync(new object[] { otherUserId }, cancellationToken);
                if (otherUser == null) continue;

                var lastMessage = await dbContext.ChatMessages
                    .Where(m => (m.SenderId == request.CurrentUserId && m.ReceiverId == otherUserId) ||
                                (m.SenderId == otherUserId && m.ReceiverId == request.CurrentUserId))
                    .OrderByDescending(m => m.Timestamp)
                    .FirstOrDefaultAsync(cancellationToken);

                conversations.Add(new ConversationDto
                {
                    UserId = otherUserId,
                    UserName = $"{otherUser.FirstName} {otherUser.LastName}",
                    UserEmail = otherUser.Email ?? string.Empty,
                    LastMessage = lastMessage?.Content,
                    LastMessageTime = lastMessage?.Timestamp,
                    LastMessageSenderId = lastMessage?.SenderId
                });
            }

            // Sort by last message time (most recent first)
            var sortedConversations = conversations.OrderByDescending(c => c.LastMessageTime).ToList();

            _logger.Information("Successfully retrieved {Count} conversations for user {UserId}", 
                sortedConversations.Count, request.CurrentUserId);

            return Results.Ok(sortedConversations);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error retrieving conversations for user {UserId}", request.CurrentUserId);
            return Results.Problem("An unexpected error occurred while retrieving conversations.");
        }
    }
}
