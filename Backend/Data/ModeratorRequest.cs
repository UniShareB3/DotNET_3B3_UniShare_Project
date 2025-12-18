using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Features.ModeratorRequest.Enums;

namespace Backend.Data;

public class ModeratorRequest
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public ModeratorRequestStatus Status { get; set; } = ModeratorRequestStatus.PENDING;

    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public Guid? ReviewedByAdminId { get; set; }

    [ForeignKey(nameof(ReviewedByAdminId))]
    public User? ReviewedByAdmin { get; set; }

    public DateTime? ReviewedDate { get; set; }
}

