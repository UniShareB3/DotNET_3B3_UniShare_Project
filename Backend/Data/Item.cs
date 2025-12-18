using Backend.Features.Items.Enums;

namespace Backend.Data;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Item
{
    /// <summary>
    /// Unique identifier for the item.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The user who owns the item (foreign key to User.Id).
    /// </summary>
    [ForeignKey(nameof(Owner))]
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Title/name of the item.
    /// </summary>
    [Required]
    [MaxLength(255)]
    [Column(TypeName = "varchar(255)")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the item.
    /// </summary>
    [Required]
    [Column(TypeName = "text")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Item category (e.g., 'Book', 'Electronics').
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column(TypeName = "varchar(50)")]
    public ItemCategory Category { get; set; } = ItemCategory.Others;

    /// <summary>
    /// Item condition (e.g., 'Excellent', 'Good', 'Fair').
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column(TypeName = "varchar(50)")]
    public ItemCondition Condition { get; set; } = ItemCondition.Good;

    /// <summary>
    /// Indicates if the item is available for booking.
    /// </summary>
    [Required]
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Timestamp of when the item was listed (UTC).
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional link to an image for the item.
    /// </summary>
    [MaxLength(500)]
    [Column(TypeName = "varchar(500)")]
    public string? ImageUrl { get; set; }

    // Navigation properties
    public User? Owner { get; set; }
    public List<Booking>? Bookings { get; set; }

    public long Price { get; set; }
}

