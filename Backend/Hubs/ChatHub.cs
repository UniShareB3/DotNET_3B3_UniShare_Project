using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Backend.Persistence;
using Backend.Data;
using System.Security.Claims;

namespace Backend.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ApplicationContext _context;

    public ChatHub(ApplicationContext context)
    {
        _context = context;
    }

    // Frontend calls this method to send a message
    public async Task SendMessage(string receiverId, string message)
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
            Content = message,
            Timestamp = DateTime.UtcNow
        };

        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();

        // 2. Send to Receiver (Live)
        // SignalR automatically maps "UserIdentifier" to the JWT "sub" or "NameIdentifier" claim
        await Clients.User(receiverId).SendAsync("ReceiveMessage", new 
        {
            SenderId = senderId,
            Content = message,
            Timestamp = chatMessage.Timestamp
        });
        
        // 3. Send back to Sender (so their UI updates immediately with the server timestamp)
        await Clients.Caller.SendAsync("ReceiveMessage", new 
        {
            SenderId = senderId,
            Content = message,
            Timestamp = chatMessage.Timestamp
        });
    }
}