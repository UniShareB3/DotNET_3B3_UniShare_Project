namespace Backend.Features.Conversations.DTO;

public class ChatMessageDto
{
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? BlobName { get; set; }
    public string ContentType { get; set; } = "text/plain";
    public DateTime Timestamp { get; set; }
}
