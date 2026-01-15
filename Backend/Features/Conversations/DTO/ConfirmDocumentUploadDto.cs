namespace Backend.Features.Conversations.DTO;

public class ConfirmDocumentUploadDto
{
    public string BlobName { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public required String ReceiverId { get; set; }
    public string? Caption { get; set; }
    public string? FileName { get; set; }
}

