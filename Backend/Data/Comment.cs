namespace Backend.Data;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Comment
{
    /// <summary>
    /// Unique identifier for the comment.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The review this comment is attached to (required).
    /// </summary>
    [ForeignKey(nameof(Review))]
    public Guid ReviewId { get; set; }

    /// <summary>
    /// The user who wrote this comment (required).
    /// </summary>
    [ForeignKey(nameof(User))]
    public Guid CommenterId { get; set; }

    /// <summary>
    /// The comment text.
    /// </summary>
    [Required]
    [MaxLength(255)]
    [Column(TypeName = "text")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of comment creation (UTC).
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp of last update (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Review? Review { get; set; }
    public User? Commenter { get; set; }
}

