using Microsoft.AspNetCore.Identity;

namespace Backend.Data;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Index(nameof(Email), IsUnique = true)]
public class User : IdentityUser<Guid>
{
    /// <summary>
    /// User's first name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// University foreign key (optional).
    /// </summary>
    [ForeignKey(nameof(University))]
    public Guid? UniversityId { get; set; }

    /// <summary>
    /// Navigation to the University entity.
    /// </summary>
    public University? University { get; set; }

    /// <summary>
    /// Timestamp of user creation in UTC.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public List<Item>? Items { get; set; }
    public List<Booking>? Bookings { get; set; }
    public List<Comment>? Comments { get; set; }

    [InverseProperty(nameof(Review.Reviewer))]
    public List<Review>? ReviewsWritten { get; set; }

    [InverseProperty(nameof(Review.TargetUser))]
    public List<Review>? ReviewsReceived { get; set; }

    public bool NewEmailConfirmed { get; set; }
    
    /// <summary>
    /// Stripe Connect Account ID for sellers
    /// </summary>
    [MaxLength(255)]
    public string? StripeAccountId { get; set; }
}
