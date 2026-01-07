using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Backend.Persistence;
using Backend.Data;
using System.Security.Claims;

namespace Backend.Hubs;

[Authorize]
public class ChatHub(ApplicationContext context) : Hub
{
    private readonly ApplicationContext _context = context;

    // Frontend calls this method to send a text message
    public async Task SendMessage(string receiverId, string message)
    {
        await SendMessageInternal(receiverId, message, null, MessageType.Text);
    }

    // Frontend calls this method to send an image message
    public async Task SendImageMessage(string receiverId, string imageUrl, string? caption = null)
    {
        await SendMessageInternal(receiverId, caption ?? "", imageUrl, MessageType.Image);
    }

    private async Task SendMessageInternal(string receiverId, string content, string? imageUrl, MessageType messageType)
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
            ImageUrl = imageUrl,
            MessageType = messageType,
            Timestamp = DateTime.UtcNow
        };

        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();

        var messageData = new
        {
            SenderId = senderId,
            Content = content,
            ImageUrl = imageUrl,
            MessageType = messageType.ToString(),
            Timestamp = chatMessage.Timestamp
        };

        // 2. Send to Receiver (Live)
        await Clients.User(receiverId).SendAsync("ReceiveMessage", messageData);

        // 3. Send back to Sender (so their UI updates immediately with the server timestamp)
        await Clients.Caller.SendAsync("ReceiveMessage", messageData);
    }
}
