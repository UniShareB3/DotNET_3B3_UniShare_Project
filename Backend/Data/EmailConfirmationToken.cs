using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Data;

public class EmailConfirmationToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(64)]
    public string Code { get; set; } = string.Empty;
    
    [Required]
    public DateTime ExpiresAt { get; set; }
    
    public bool IsUsed { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } 
    
    // Navigation property
    public User? User { get; set; }
}
