namespace Backend.Features.Conversations.DTO;

public class ConfirmDocumentUploadDto
{
    public string BlobName { get; set; } = string.Empty;
    public required String ReceiverId { get; set; }
    public string? Caption { get; set; }
}

