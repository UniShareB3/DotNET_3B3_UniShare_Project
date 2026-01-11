using Backend.Features.Conversations.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Conversations.GetChatHistory;

public class GetChatHistoryHandler(ApplicationContext dbContext) : IRequestHandler<GetChatHistoryRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GetChatHistoryHandler>();

    public async Task<IResult> Handle(GetChatHistoryRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving chat history between user {CurrentUserId} and user {OtherUserId}", 
            request.CurrentUserId, request.OtherUserId);

        try
        {
            var messages = await dbContext.ChatMessages
                .Where(m => (m.SenderId == request.CurrentUserId && m.ReceiverId == request.OtherUserId) ||
                            (m.SenderId == request.OtherUserId && m.ReceiverId == request.CurrentUserId))
                .OrderBy(m => m.Timestamp)
                .Select(m => new ChatMessageDto
                {
                    SenderId = m.SenderId,
                    Content = m.Content,
                    BlobName = m.BlobName,
                    ContentType = m.ContentType,
                    Timestamp = m.Timestamp
                })
                .ToListAsync(cancellationToken);

            _logger.Information("Successfully retrieved {Count} messages between users", messages.Count);
            return Results.Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error retrieving chat history between user {CurrentUserId} and user {OtherUserId}",
                request.CurrentUserId, request.OtherUserId);
            return Results.Problem("An unexpected error occurred while retrieving chat history.");
        }
    }
}
