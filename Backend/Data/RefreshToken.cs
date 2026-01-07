using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Data;

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(255)]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime ExpiresAt { get; set; }
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    
    public bool IsRevoked { get; set; }
    
    public DateTime? RevokedAt { get; set; }
    
    [MaxLength(255)]
    public string? ReasonRevoked { get; set; }
    
    /// <summary>
    /// Token family identifier - all tokens in a rotation chain share the same family ID
    /// </summary>
    [Required]
    public Guid TokenFamily { get; set; }
    
    /// <summary>
    /// Reference to the parent token that was exchanged to create this token (null for initial login tokens)
    /// </summary>
    public Guid? ParentTokenId { get; set; }
    
    /// <summary>
    /// Reference to the child token that was created by exchanging this token (null if not yet rotated)
    /// </summary>
    public Guid? ReplacedByTokenId { get; set; }
}
