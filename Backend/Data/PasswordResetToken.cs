using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Data;

public class PasswordResetToken
{
  [Key]
  public Guid Id { get; set; } = Guid.NewGuid();

  [Required]
  [ForeignKey(nameof(User))]
  public Guid UserId { get; set; }

  [Required]
  [MaxLength(500)] // Increased to accommodate full UserManager token
  public string Code { get; set; } = string.Empty;

  [Required]
  public DateTime ExpiresAt { get; set; }

  public bool IsUsed { get; set; } = false;

  [Required]
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  // Navigation property
  public User? User { get; set; }
}
