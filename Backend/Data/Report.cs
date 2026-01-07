using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Features.Reports.Enums;

namespace Backend.Data;

public class Report
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ItemId { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item Item { get; set; } = null!;

    [Required]
    public Guid OwnerId { get; set; }

    [ForeignKey(nameof(OwnerId))]
    public User Owner { get; set; } = null!;

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public Guid? ModeratorId { get; set; }

    [ForeignKey(nameof(ModeratorId))]
    public User? Moderator { get; set; }
}
