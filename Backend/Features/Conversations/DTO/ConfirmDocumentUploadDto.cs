namespace Backend.Features.Conversations.DTO;

public class ConfirmDocumentUploadDto
{
    public string BlobName { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public required string ReceiverId { get; set; }
    public string? Caption { get; set; }
}

