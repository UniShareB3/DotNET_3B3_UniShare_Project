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
    public string Content { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}