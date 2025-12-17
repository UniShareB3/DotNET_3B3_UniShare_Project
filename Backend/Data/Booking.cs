using Backend.Features.Bookings.Enums;

namespace Backend.Data;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Booking
{
    /// <summary>
    /// Unique identifier for the booking.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The item being booked (foreign key to Item.Id).
    /// </summary>
    [ForeignKey(nameof(Item))]
    public Guid ItemId { get; set; }

    /// <summary>
    /// The user borrowing the item (foreign key to User.Id).
    /// </summary>
    [ForeignKey(nameof(User))]
    public Guid BorrowerId { get; set; }

    /// <summary>
    /// Timestamp of when the request was made. Defaults to UTC now.
    /// </summary>
    [Required]
    public DateTime RequestedOn { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The planned start date of the loan.
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The planned return date of the loan.
    /// </summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Booking status: Pending, Approved, Rejected, Completed, Canceled.
    /// Stored as a string (VARCHAR(50)).
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column(TypeName = "varchar(50)")]
    public BookingStatus BookingStatus { get; set; } = BookingStatus.Pending;

    /// <summary>
    /// Timestamp of when the booking was approved by the owner.
    /// </summary>
    public DateTime? ApprovedOn { get; set; }

    /// <summary>
    /// Timestamp of when the item was marked as returned/completed.
    /// </summary>
    public DateTime? CompletedOn { get; set; }
    
    /// <summary>
    /// Indicates whether the payment for this booking has been secured via Stripe.
    /// </summary>
    [Required]
    public bool IsPaid { get; set; } = false;

    // Navigation properties (optional)
    public Item? Item { get; set; }
}