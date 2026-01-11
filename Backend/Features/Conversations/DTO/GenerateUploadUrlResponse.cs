namespace Backend.Features.Conversations.DTO;

public class GenerateUploadUrlResponse
{
    public string UploadUrl { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

