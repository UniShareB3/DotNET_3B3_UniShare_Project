namespace Backend.Features.Conversations.DTO;

public class ChatMessageDto
{
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

