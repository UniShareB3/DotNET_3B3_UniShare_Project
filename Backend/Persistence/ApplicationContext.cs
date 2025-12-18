using Backend.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Backend.Persistence;

public class ApplicationContext(DbContextOptions<ApplicationContext> options) : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<University> Universities { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<EmailConfirmationToken> EmailConfirmationTokens { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<ModeratorRequest> ModeratorRequests { get; set; }
    
    public DbSet<ChatMessage> ChatMessages { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<Item>()
            .Property(i=> i.Category)
            .HasConversion<string>();
        
        builder.Entity<Item>()
            .Property(i=> i.Condition)
            .HasConversion<string>();
        
        // Configure Report relationships
        builder.Entity<Report>()
            .HasOne(r => r.Owner)
            .WithMany()
            .HasForeignKey(r => r.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<Report>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<Report>()
            .HasOne(r => r.Moderator)
            .WithMany()
            .HasForeignKey(r => r.ModeratorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Rename AspNetUsers table to Users
        builder.Entity<User>(b =>
        {
            b.ToTable("Users");
        });

        // Rename AspNetRoles table to Roles
        builder.Entity<IdentityRole<Guid>>(b =>
        {
            b.ToTable("Roles");
        });

        // Rename AspNetUserClaims table to UserClaims
        builder.Entity<IdentityUserClaim<Guid>>(b =>
        {
            b.ToTable("UserClaims");
        });

        // Rename AspNetUserLogins table to UserLogins
        builder.Entity<IdentityUserLogin<Guid>>(b =>
        {
            b.ToTable("UserLogins");
        });

        // Rename AspNetUserTokens table to UserTokens
        builder.Entity<IdentityUserToken<Guid>>(b =>
        {
            b.ToTable("UserTokens");
        });

        // Rename AspNetRoleClaims table to RoleClaims
        builder.Entity<IdentityRoleClaim<Guid>>(b =>
        {
            b.ToTable("RoleClaims");
        });

        // Rename AspNetUserRoles table to UserRoles
        builder.Entity<IdentityUserRole<Guid>>(b =>
        {
            b.ToTable("UserRoles");
        });
        
        // Configure ChatMessage relationships
        builder.Entity<ChatMessage>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent deleting user from deleting messages

        builder.Entity<ChatMessage>()
            .HasOne(m => m.Receiver)
            .WithMany()
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
