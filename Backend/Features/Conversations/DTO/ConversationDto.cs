namespace Backend.Features.Conversations.DTO;

public class ConversationDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? LastMessage { get; set; }
    public DateTime? LastMessageTime { get; set; }
    public Guid? LastMessageSenderId { get; set; }
}

