namespace Backend.Features.Conversations.DTO;

public class DocumentUrlResponseDto
{
    public string BlobName { get; set; } = string.Empty;
    public string DocumentUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

