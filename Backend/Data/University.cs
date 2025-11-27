using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data
{
    /// <summary>
    /// Represents a university supported by the platform.
    /// </summary>
    [Index(nameof(Name), IsUnique = true)]
    [Index(nameof(ShortCode), IsUnique = true)]
    [Index(nameof(EmailDomain), IsUnique = true)]
    public class University
    {
        /// <summary>
        /// Primary key - unique identifier for the university.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Full official name of the university.
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Common acronym or short code (e.g., 'UAIC').
        /// </summary>
        [MaxLength(10)]
        public string? ShortCode { get; set; }

        /// <summary>
        /// Official email domain used for registration validation (e.g., '@uaic.edu').
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string EmailDomain { get; set; } = null!;

        /// <summary>
        /// Timestamp of record creation in UTC.
         /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}