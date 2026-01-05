using Backend.Data;
using Backend.Features.Bookings.Enums;
using Backend.Features.Items.Enums;
using Backend.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Backend.Tests.Seeder;

/// <summary>
/// Provides consistent, predefined test data for integration tests.
/// This ensures all tests have access to the same users, items, and other entities.
/// </summary>
public static class TestDataSeeder
{
    // Predefined test credentials - always consistent across test runs
    public const string AdminEmail = "admin@student.uaic.ro";
    public const string AdminPassword = "Admin@1234";
    public static readonly Guid AdminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    
    public const string ModeratorEmail = "moderator@student.uaic.ro";
    public const string ModeratorPassword = "Moderator@1234";
    public static readonly Guid ModeratorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    
    public const string UserEmail = "user@student.uaic.ro";
    public const string UserPassword = "User@1234";
    public static readonly Guid UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    
    public const string UnverifiedUserEmail = "unverified@student.uaic.ro";
    public const string UnverifiedUserPassword = "Unverified@1234";
    public static readonly Guid UnverifiedUserId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    // Predefined university IDs
    public static readonly Guid UaicId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid TuiasiId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid UpbId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    // Predefined item IDs
    public static readonly Guid LaptopItemId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid BookItemId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid JacketItemId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    // Predefined booking IDs
    public static readonly Guid PendingBookingId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public static readonly Guid ApprovedBookingId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    public static readonly Guid CompletedBookingId = Guid.Parse("20000000-0000-0000-0000-000000000003");

    // Predefined review IDs
    public static readonly Guid ItemReviewId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    public static readonly Guid UserReviewId = Guid.Parse("30000000-0000-0000-0000-000000000002");

    /// <summary>
    /// Seeds the test database with predefined test data
    /// </summary>
    public static async Task SeedTestDataAsync(
        ApplicationContext context,
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        // 1. Seed Roles
        await SeedRoles(roleManager);
        
        // 2. Seed Universities
        var universities = await SeedUniversities(context);
        
        // 3. Seed Test Users (static users only)
        var users = await SeedTestUsers(userManager, universities);
        
        // 4. Seed Test Items
        await SeedTestItems(context);
        
        // 5. Seed Test Bookings
        await SeedTestBookings(context, users);
        
        // 6. Seed Test Reviews
        await SeedTestReviews(context);
        
        await context.SaveChangesAsync();
    }

    private static async Task SeedRoles(RoleManager<IdentityRole<Guid>> roleManager)
    {
        var roles = new[] { "Admin", "Moderator", "User" };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }
    }

    private static async Task<List<University>> SeedUniversities(ApplicationContext context)
    {
        if (context.Universities.Any())
        {
            return context.Universities.ToList();
        }

        var universities = new List<University>
        {
            new University
            {
                Id = UaicId,
                Name = "Universitatea Alexandru Ioan Cuza",
                ShortCode = "UAIC",
                EmailDomain = "@uaic.ro",
                CreatedAt = DateTime.UtcNow
            },
            new University
            {
                Id = TuiasiId,
                Name = "Universitatea Tehnică Gheorghe Asachi",
                ShortCode = "TUIASI",
                EmailDomain = "@tuiasi.ro",
                CreatedAt = DateTime.UtcNow
            },
            new University
            {
                Id = UpbId,
                Name = "Universitatea Politehnica București",
                ShortCode = "UPB",
                EmailDomain = "@upb.ro",
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Universities.AddRange(universities);
        await context.SaveChangesAsync();
        
        return universities;
    }

    private static async Task<List<User>> SeedTestUsers(
        UserManager<User> userManager,
        List<University> universities)
    {
        var users = new List<User>();
        var uaicUniversity = universities.First(u => u.ShortCode == "UAIC");

        // 1. Admin User (verified, all permissions)
        var admin = await CreateUserIfNotExists(userManager, new User
        {
            Id = AdminId,
            FirstName = "Admin",
            LastName = "User",
            Email = AdminEmail,
            UserName = AdminEmail,
            NormalizedEmail = AdminEmail.ToUpper(),
            NormalizedUserName = AdminEmail.ToUpper(),
            EmailConfirmed = true,
            UniversityId = uaicUniversity.Id,
            CreatedAt = DateTime.UtcNow
        }, AdminPassword, new[] { "Admin", "User" });
        
        if (admin != null) users.Add(admin);

        // 2. Moderator User (verified, can moderate content)
        var moderator = await CreateUserIfNotExists(userManager, new User
        {
            Id = ModeratorId,
            FirstName = "Moderator",
            LastName = "User",
            Email = ModeratorEmail,
            UserName = ModeratorEmail,
            NormalizedEmail = ModeratorEmail.ToUpper(),
            NormalizedUserName = ModeratorEmail.ToUpper(),
            EmailConfirmed = true,
            UniversityId = uaicUniversity.Id,
            CreatedAt = DateTime.UtcNow
        }, ModeratorPassword, new[] { "Moderator", "User" });
        
        if (moderator != null) users.Add(moderator);

        // 3. Regular User (verified, standard permissions)
        var user = await CreateUserIfNotExists(userManager, new User
        {
            Id = UserId,
            FirstName = "Regular",
            LastName = "User",
            Email = UserEmail,
            UserName = UserEmail,
            NormalizedEmail = UserEmail.ToUpper(),
            NormalizedUserName = UserEmail.ToUpper(),
            EmailConfirmed = true,
            UniversityId = uaicUniversity.Id,
            CreatedAt = DateTime.UtcNow
        }, UserPassword, new[] { "User" });
        
        if (user != null) users.Add(user);

        // 4. Unverified User (NOT verified - for testing email verification flows)
        var unverifiedUser = await CreateUserIfNotExists(userManager, new User
        {
            Id = UnverifiedUserId,
            FirstName = "Unverified",
            LastName = "User",
            Email = UnverifiedUserEmail,
            UserName = UnverifiedUserEmail,
            NormalizedEmail = UnverifiedUserEmail.ToUpper(),
            NormalizedUserName = UnverifiedUserEmail.ToUpper(),
            EmailConfirmed = false, // NOT VERIFIED
            UniversityId = uaicUniversity.Id,
            CreatedAt = DateTime.UtcNow
        }, UnverifiedUserPassword, new[] { "User" });
        
        if (unverifiedUser != null) users.Add(unverifiedUser);

        return users;
    }

    private static async Task<User?> CreateUserIfNotExists(
        UserManager<User> userManager,
        User user,
        string password,
        string[] roles)
    {
        var existingUser = await userManager.FindByEmailAsync(user.Email!);
        if (existingUser != null)
        {
            return existingUser;
        }

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            foreach (var role in roles)
            {
                await userManager.AddToRoleAsync(user, role);
            }
            return user;
        }

        return null;
    }

    private static async Task SeedTestItems(ApplicationContext context)
    {
        if (context.Items.Any())
        {
            return;
        }

        var verifiedUser = UserId;
        if (verifiedUser == null) return;

        var items = new List<Item>
        {
            new Item
            {
                Id = LaptopItemId,
                Name = "Test Laptop",
                Description = "A test laptop for integration tests",
                Category = ItemCategory.Electronics,
                Condition = ItemCondition.Good,
                IsAvailable = true,
                OwnerId = verifiedUser,
                CreatedAt = DateTime.UtcNow
            },
            new Item
            {
                Id = BookItemId,
                Name = "Test Book",
                Description = "A test book for integration tests",
                Category = ItemCategory.Books,
                Condition = ItemCondition.New,
                IsAvailable = true,
                OwnerId = verifiedUser,
                CreatedAt = DateTime.UtcNow
            },
            new Item
            {
                Id = JacketItemId,
                Name = "Test Jacket",
                Description = "A test jacket for integration tests",
                Category = ItemCategory.Clothing,
                Condition = ItemCondition.Good,
                IsAvailable = false, 
                OwnerId = verifiedUser,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Items.AddRange(items);
        await context.SaveChangesAsync();
    }

    private static async Task SeedTestBookings(ApplicationContext context, List<User> users)
    {
        if (context.Bookings.Any())
        {
            return;
        }

        var itemOwner = users.FirstOrDefault(u => u.Email == UserEmail);
        var borrower = users.FirstOrDefault(u => u.Email == ModeratorEmail);
        
        if (itemOwner == null || borrower == null) return;

        var bookings = new List<Booking>
        {
            new Booking
            {
                Id = PendingBookingId,
                ItemId = LaptopItemId,
                BorrowerId = borrower.Id,
                RequestedOn = DateTime.UtcNow.AddDays(-2),
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(7),
                BookingStatus = BookingStatus.Pending
            },
            new Booking
            {
                Id = ApprovedBookingId,
                ItemId = BookItemId,
                BorrowerId = borrower.Id,
                RequestedOn = DateTime.UtcNow.AddDays(-5),
                StartDate = DateTime.UtcNow.AddDays(-3),
                EndDate = DateTime.UtcNow.AddDays(3),
                BookingStatus = BookingStatus.Approved,
                ApprovedOn = DateTime.UtcNow.AddDays(-4)
            },
            new Booking
            {
                Id = CompletedBookingId,
                ItemId = BookItemId,
                BorrowerId = borrower.Id,
                RequestedOn = DateTime.UtcNow.AddDays(-15),
                StartDate = DateTime.UtcNow.AddDays(-14),
                EndDate = DateTime.UtcNow.AddDays(-7),
                BookingStatus = BookingStatus.Completed,
                ApprovedOn = DateTime.UtcNow.AddDays(-14),
                CompletedOn = DateTime.UtcNow.AddDays(-7)
            }
        };

        context.Bookings.AddRange(bookings);
        await context.SaveChangesAsync();
    }

    private static async Task SeedTestReviews(ApplicationContext context)
    {
        if (context.Reviews.Any())
        {
            return;
        }

        var reviews = new List<Review>
        {
            // Review for an Item (after completed booking)
            new Review
            {
                Id = ItemReviewId,
                BookingId = CompletedBookingId,
                ReviewerId = ModeratorId, // Borrower reviews the item
                TargetItemId = BookItemId,
                TargetUserId = null,
                Rating = 5,
                Comment = "Great book, exactly as described!",
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            },
            // Review for a User (owner reviews borrower)
            new Review
            {
                Id = UserReviewId,
                BookingId = CompletedBookingId,
                ReviewerId = UserId, // Owner reviews the borrower
                TargetUserId = ModeratorId,
                TargetItemId = null,
                Rating = 4,
                Comment = "Returned the item on time and in good condition.",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            }
        };

        context.Reviews.AddRange(reviews);
        await context.SaveChangesAsync();
    }
    
    public static void ClearDatabase(ApplicationContext context)
    {
        context.Reviews.RemoveRange(context.Reviews);
        context.Bookings.RemoveRange(context.Bookings);
        context.RefreshTokens.RemoveRange(context.RefreshTokens);
        context.EmailConfirmationTokens.RemoveRange(context.EmailConfirmationTokens);
        context.Items.RemoveRange(context.Items);
        context.Users.RemoveRange(context.Users);
        context.Universities.RemoveRange(context.Universities);
        context.SaveChanges();
    }
}

