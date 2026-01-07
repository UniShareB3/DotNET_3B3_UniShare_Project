﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Data;

public enum MessageType
{
    Text = 0,
    Image = 1
}

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

    /// <summary>
    /// URL of the image if MessageType is Image
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Type of message: Text or Image
    /// </summary>
    public MessageType MessageType { get; set; } = MessageType.Text;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}