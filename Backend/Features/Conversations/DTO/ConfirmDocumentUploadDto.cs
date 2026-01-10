namespace Backend.Features.Conversations.DTO;

public class ConfirmDocumentUploadDto
{
    public string BlobName { get; set; } = string.Empty;
    public Guid ReceiverId { get; set; }
    public string? Caption { get; set; }
}

