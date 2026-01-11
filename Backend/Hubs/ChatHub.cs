using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Backend.Persistence;
using Backend.Data;
using Backend.Services.ContentType;
using System.Security.Claims;

namespace Backend.Hubs;

[Authorize]
public class ChatHub(ApplicationContext context) : Hub
{
    private readonly ApplicationContext _context = context;

    // Frontend calls this method to send a text message
    public async Task SendMessage(string receiverId, string message)
    {
        await SendMessageInternal(receiverId, message, null, ContentTypeResolver.TextPlain);
    }

    // Frontend calls this method to broadcast an image/document message
    // Note: The message is already saved to DB by ConfirmDocumentUploadHandler
    // This method only broadcasts the message to clients in real-time
    public async Task SendImageMessage(string receiverId, string blobName, string documentUrl, string? caption = null)
    {
        var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(senderId))
        {
            return;
        }

        // Determine content type from file extension
        var contentType = ContentTypeResolver.FromFileName(blobName);

        var messageData = new
        {
            SenderId = senderId,
            Content = caption ?? "",
            BlobName = blobName,
            DocumentUrl = documentUrl,
            ContentType = contentType,
            Timestamp = DateTime.UtcNow
        };

        // Broadcast to receiver (live update)
        await Clients.User(receiverId).SendAsync("ReceiveMessage", messageData);

        // Broadcast to sender (so their UI updates immediately)
        await Clients.Caller.SendAsync("ReceiveMessage", messageData);
    }

    private async Task SendMessageInternal(string receiverId, string content, string? documentUrl, string contentType)
    {
        var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(senderId) || !Guid.TryParse(senderId, out var senderGuid) || !Guid.TryParse(receiverId, out var receiverGuid))
        {
            return;
        }

        // 1. Save to Database
        var chatMessage = new ChatMessage
        {
            SenderId = senderGuid,
            ReceiverId = receiverGuid,
            Content = content,
            BlobName = documentUrl,
            ContentType = contentType,
            Timestamp = DateTime.UtcNow
        };

        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();

        var messageData = new
        {
            SenderId = senderId,
            Content = content,
            BlobName = documentUrl,
            ContentType = contentType,
            Timestamp = chatMessage.Timestamp
        };

        // 2. Send to Receiver (Live)
        await Clients.User(receiverId).SendAsync("ReceiveMessage", messageData);

        // 3. Send back to Sender (so their UI updates immediately with the server timestamp)
        await Clients.Caller.SendAsync("ReceiveMessage", messageData);
    }
}
