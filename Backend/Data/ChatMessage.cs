using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Data;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SenderId { get; set; }
    public User? Sender { get; set; }

    [Required]
    public Guid ReceiverId { get; set; }
    public User? Receiver { get; set; }

    [Required]
    [MaxLength(255)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Blob storage name/path for the document/image (e.g., "chat-documents/guid/filename.png")
    /// </summary>
    public string? BlobName { get; set; }

    /// <summary>
    /// MIME type of the message content (e.g., "text/plain", "image/jpeg", "application/pdf")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = "text/plain";

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}