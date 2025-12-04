namespace Backend.Data;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Review
{
    /// <summary>
    /// Unique identifier for the review.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The booking this review relates to (unique, required).
    /// </summary>
    [ForeignKey(nameof(Booking))]
    public Guid BookingId { get; set; }

    /// <summary>
    /// The user who wrote the review.
    /// </summary>
    [ForeignKey(nameof(Reviewer))]
    public Guid ReviewerId { get; set; }

    /// <summary>
    /// The user being reviewed (owner or borrower). Nullable when review targets an item.
    /// </summary>
    [ForeignKey(nameof(TargetUser))]
    public Guid? TargetUserId { get; set; }

    /// <summary>
    /// The item being reviewed. Nullable when review targets a user.
    /// </summary>
    [ForeignKey(nameof(TargetItem))]
    public Guid? TargetItemId { get; set; }

    /// <summary>
    /// Star rating from 1 to 5.
    /// </summary>
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    /// <summary>
    /// Optional textual feedback.
    /// </summary>
    [Column(TypeName = "text")]
    public string? Comment { get; set; }

    /// <summary>
    /// Timestamp of review creation (UTC).
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Booking? Booking { get; set; }

    [InverseProperty(nameof(User.ReviewsWritten))]
    public User? Reviewer { get; set; }

    [InverseProperty(nameof(User.ReviewsReceived))]
    public User? TargetUser { get; set; }

    public Item? TargetItem { get; set; }
}