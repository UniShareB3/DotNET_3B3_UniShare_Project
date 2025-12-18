using Backend.Features.Items.Enums;
using Backend.Persistence;
using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Backend.Data;

public static class DatabaseSeeder
{
    private static readonly Serilog.ILogger _logger = Log.ForContext(typeof(DatabaseSeeder));

    public static async Task SeedAsync(
        ApplicationContext context,
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        try
        {
            _logger.Information("Starting database seeding...");

            // 1. Seed Roles
            await SeedRoles(roleManager);

            // 2. Seed Universities
            var universities = await SeedUniversities(context);

            // 3. Seed Admin Account (fixed admin user)
            await SeedAdminAccount(context, userManager, universities);

            // 4. Seed Users (Moderators and regular Users only)
            var users = await SeedUsers(context, userManager, universities);

            // 5. Seed Items (100 items)
            await SeedItems(context, users);

            _logger.Information("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedRoles(RoleManager<IdentityRole<Guid>> roleManager)
    {
        _logger.Information("Seeding roles...");

        var roles = new[] { "Admin", "Moderator", "User", "Seller" };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole<Guid>(roleName);
                await roleManager.CreateAsync(role);
                _logger.Information("Created role: {RoleName}", roleName);
            }
            else
            {
                _logger.Information("Role already exists: {RoleName}", roleName);
            }
        }
    }

    private static async Task<List<University>> SeedUniversities(ApplicationContext context)
    {
        _logger.Information("Seeding universities...");

        if (await context.Universities.AnyAsync())
        {
            _logger.Information("Universities already exist, skipping seeding");
            return await context.Universities.ToListAsync();
        }

        var universities = new List<University>
        {
            new University
            {
                Id = Guid.NewGuid(),
                Name = "Universitatea Alexandru Ioan Cuza",
                ShortCode = "UAIC",
                EmailDomain = "@student.uaic.ro",
                CreatedAt = DateTime.UtcNow
            },
            new University
            {
                Id = Guid.NewGuid(),
                Name = "Universitatea Tehnică Gheorghe Asachi",
                ShortCode = "TUIASI",
                EmailDomain = "@tuiasi.ro",
                CreatedAt = DateTime.UtcNow
            },
            new University
            {
                Id = Guid.NewGuid(),
                Name = "Universitatea Politehnica București",
                ShortCode = "UPB",
                EmailDomain = "@upb.ro",
                CreatedAt = DateTime.UtcNow
            },
            new University
            {
                Id = Guid.NewGuid(),
                Name = "Universitatea Babeș-Bolyai",
                ShortCode = "UBB",
                EmailDomain = "@ubbcluj.ro",
                CreatedAt = DateTime.UtcNow
            },
            new University
            {
                Id = Guid.NewGuid(),
                Name = "Universitatea din București",
                ShortCode = "UB",
                EmailDomain = "@unibuc.ro",
                CreatedAt = DateTime.UtcNow
            },
            new University
            {
                Id = Guid.NewGuid(),
                Name = "Universitatea de Vest din Timișoara",
                ShortCode = "UVT",
                EmailDomain = "@e-uvt.ro",
                CreatedAt = DateTime.UtcNow
            },
            new University
            {
                Id = Guid.NewGuid(),
                Name = "Universitatea Transilvania din Brașov",
                ShortCode = "UNITBV",
                EmailDomain = "@unitbv.ro",
                CreatedAt = DateTime.UtcNow
            },
            new University
            {
                Id = Guid.NewGuid(),
                Name = "Universitatea din Craiova",
                ShortCode = "UCRAV",
                EmailDomain = "@ucv.ro",
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Universities.AddRange(universities);
        await context.SaveChangesAsync();

        _logger.Information("Seeded {Count} universities", universities.Count);
        return universities;
    }

    private static async Task SeedAdminAccount(
        ApplicationContext context,
        UserManager<User> userManager,
        List<University> universities)
    {
        _logger.Information("Seeding admin account...");

        const string adminEmail = "admin@uaic.ro";
        const string adminPassword = "Admin@1234";
        const string adminFirstName = "Admin";
        const string adminLastName = "UniShare";

        // Check if admin already exists
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin != null)
        {
            _logger.Information("Admin account already exists: {Email}", adminEmail);
            return;
        }

        // Get UAIC university for admin (or first university as fallback)
        var adminUniversity = universities.FirstOrDefault(u => u.ShortCode == "UAIC") 
                             ?? universities.First();

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = adminFirstName,
            LastName = adminLastName,
            Email = adminEmail,
            UserName = adminEmail,
            NormalizedEmail = adminEmail.ToUpper(),
            NormalizedUserName = adminEmail.ToUpper(),
            EmailConfirmed = true, // Admin is always verified
            UniversityId = adminUniversity.Id,
            CreatedAt = DateTime.UtcNow,
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            LockoutEnabled = false
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);

        if (result.Succeeded)
        {
            // Assign both User and Admin roles
            await userManager.AddToRoleAsync(adminUser, "User");
            await userManager.AddToRoleAsync(adminUser, "Admin");

            _logger.Information("✅ Created admin account: {Email} with password: {Password}", 
                adminEmail, adminPassword);
        }
        else
        {
            _logger.Error("Failed to create admin account: {Errors}", 
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task<List<User>> SeedUsers(
        ApplicationContext context,
        UserManager<User> userManager,
        List<University> universities)
    {
        _logger.Information("Seeding users (Moderators and regular Users)...");

        const int targetUserCount = 10; // 1 Moderator + 9 regular Users
        
        // Count existing non-admin users (users without Admin role)
        var allUsers = await context.Users.ToListAsync();
        var nonAdminUsers = new List<User>();
        
        foreach (var user in allUsers)
        {
            var roles = await userManager.GetRolesAsync(user);
            if (!roles.Contains("Admin"))
            {
                nonAdminUsers.Add(user);
            }
        }
        
        var existingUserCount = nonAdminUsers.Count;

        if (existingUserCount >= targetUserCount)
        {
            _logger.Information("Non-admin user count ({ExistingCount}) meets or exceeds target ({TargetCount}), skipping user seeding", 
                existingUserCount, targetUserCount);
            return await context.Users.ToListAsync();
        }

        var usersToCreate = targetUserCount - existingUserCount;
        _logger.Information("Found {ExistingCount} non-admin users, creating {ToCreate} more to reach target of {TargetCount}", 
            existingUserCount, usersToCreate, targetUserCount);

        var users = new List<User>();
        var random = new Random();

        // Create faker for generating realistic data
        Randomizer.Seed = new Random(12345 + existingUserCount); // Different seed based on existing count

        var userFaker = new Faker<User>()
            .RuleFor(u => u.Id, f => Guid.NewGuid())
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.CreatedAt, f => f.Date.Past(1, DateTime.UtcNow))
            .RuleFor(u => u.EmailConfirmed, f => f.Random.Bool(0.7f)) // 70% will have email verified
            .RuleFor(u => u.PhoneNumberConfirmed, f => false)
            .RuleFor(u => u.TwoFactorEnabled, f => false)
            .RuleFor(u => u.LockoutEnabled, f => false);

        // Check if we need to create a moderator (only if no moderators exist)
        var hasModerator = nonAdminUsers.Any(u => userManager.GetRolesAsync(u).Result.Contains("Moderator"));

        for (int i = 0; i < usersToCreate; i++)
        {
            var university = universities[random.Next(universities.Count)];
            var user = userFaker.Generate();

            // Set university
            user.UniversityId = university.Id;

            // Generate email that matches university domain
            var emailPrefix = $"{user.FirstName.ToLower()}.{user.LastName.ToLower()}{random.Next(1, 999)}";
            var isStudent = random.Next(0, 2) == 0; // 50% chance to be student
            
            if (isStudent && university.EmailDomain.Contains("@"))
            {
                var domain = university.EmailDomain.Substring(1);
                user.Email = $"{emailPrefix}@student.{domain}";
            }
            else
            {
                user.Email = $"{emailPrefix}{university.EmailDomain}";
            }

            user.UserName = user.Email;
            user.NormalizedEmail = user.Email.ToUpper();
            user.NormalizedUserName = user.Email.ToUpper();

            // Create user with password
            var password = "Test@1234"; // All users have the same password for testing
            var result = await userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // Assign default "User" role
                await userManager.AddToRoleAsync(user, "User");

                // Make first non-admin user a moderator (only if no moderator exists)
                if (!hasModerator && i == 0)
                {
                    await userManager.AddToRoleAsync(user, "Moderator");
                    hasModerator = true;
                    _logger.Information("Created moderator user: {Email} (Verified: {Verified})", 
                        user.Email, user.EmailConfirmed);
                }
                else
                {
                    _logger.Information("Created regular user: {Email} (Verified: {Verified})", 
                        user.Email, user.EmailConfirmed);
                }

                users.Add(user);
            }
            else
            {
                _logger.Error("Failed to create user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        _logger.Information("Seeded {Count} new users (Total non-admin: {Total})", users.Count, existingUserCount + users.Count);
        
        // Return all users (existing + new)
        return await context.Users.ToListAsync();
    }

    private static async Task SeedItems(ApplicationContext context, List<User> users)
    {
        _logger.Information("Seeding items...");

        const int targetItemCount = 100;
        var existingItemCount = await context.Items.CountAsync();

        if (existingItemCount >= targetItemCount)
        {
            _logger.Information("Item count ({ExistingCount}) meets or exceeds target ({TargetCount}), skipping item seeding", 
                existingItemCount, targetItemCount);
            return;
        }

        var itemsToCreate = targetItemCount - existingItemCount;
        _logger.Information("Found {ExistingCount} items, creating {ToCreate} more to reach target of {TargetCount}", 
            existingItemCount, itemsToCreate, targetItemCount);

        var random = new Random(12345 + existingItemCount); // Different seed based on existing count
        Randomizer.Seed = new Random(12345 + existingItemCount);

        // Item name templates by category
        var bookTitles = new[] { "Introduction to Programming", "Data Structures and Algorithms", 
            "Calculus I", "Physics Fundamentals", "Chemistry Basics", "English Literature", 
            "Modern History", "Psychology 101", "Biology Textbook", "Economics Principles" };

        var electronicItems = new[] { "USB-C Cable", "Wireless Mouse", "Keyboard", "Headphones", 
            "Laptop Charger", "Phone Holder", "HDMI Cable", "USB Hub", "Webcam", "External Hard Drive" };

        var kitchenItems = new[] { "Coffee Mug", "Water Bottle", "Lunch Box", "Food Container", 
            "Cutlery Set", "Plate", "Bowl", "Frying Pan", "Pot", "Kitchen Knife" };

        var clothingItems = new[] { "T-Shirt", "Hoodie", "Jeans", "Jacket", "Sweater", 
            "Scarf", "Hat", "Gloves", "Socks", "Sports Shoes" };

        var accessories = new[] { "Backpack", "Laptop Bag", "Umbrella", "Sunglasses", 
            "Watch", "Wallet", "Pen Set", "Notebook", "Calculator", "Water Flask" };

        var itemFaker = new Faker<Item>()
            .RuleFor(i => i.Id, f => Guid.NewGuid())
            .RuleFor(i => i.CreatedAt, f => f.Date.Past(6, DateTime.UtcNow.AddMonths(-1)))
            .RuleFor(i => i.IsAvailable, f => f.Random.Bool(0.8f)) // 80% available
            .RuleFor(i => i.ImageUrl, f => f.Random.Bool(0.5f) ? f.Image.PicsumUrl() : null);

        var items = new List<Item>();

        for (int i = 0; i < itemsToCreate; i++)
        {
            var item = itemFaker.Generate();
            
            // Assign random owner
            var owner = users[random.Next(users.Count)];
            item.OwnerId = owner.Id;

            // Assign category
            var category = (ItemCategory)random.Next(0, 6);
            item.Category = category;

            // Set name based on category
            item.Name = category switch
            {
                ItemCategory.Books => bookTitles[random.Next(bookTitles.Length)] + $" (Edition {random.Next(1, 6)})",
                ItemCategory.Electronics => electronicItems[random.Next(electronicItems.Length)],
                ItemCategory.Kitchen => kitchenItems[random.Next(kitchenItems.Length)],
                ItemCategory.Clothing => clothingItems[random.Next(clothingItems.Length)] + $" Size {new[] { "S", "M", "L", "XL" }[random.Next(4)]}",
                ItemCategory.Accessories => accessories[random.Next(accessories.Length)],
                _ => $"Miscellaneous Item {existingItemCount + i + 1}"
            };

            // Generate description
            var faker = new Faker();
            item.Description = category switch
            {
                ItemCategory.Books => $"Academic textbook in {new[] { "good", "excellent", "fair" }[random.Next(3)]} condition. {faker.Lorem.Sentence(10)}",
                ItemCategory.Electronics => $"Electronic device in working condition. {faker.Lorem.Sentence(8)}",
                ItemCategory.Kitchen => $"Kitchen item, {new[] { "lightly used", "like new", "well-maintained" }[random.Next(3)]}. {faker.Lorem.Sentence(7)}",
                ItemCategory.Clothing => $"Clothing item, {new[] { "barely worn", "gently used", "in good shape" }[random.Next(3)]}. {faker.Lorem.Sentence(6)}",
                ItemCategory.Accessories => $"Useful accessory for daily use. {faker.Lorem.Sentence(8)}",
                _ => faker.Lorem.Sentences(2)
            };

            // Assign condition
            item.Condition = (ItemCondition)random.Next(0, 5);

            items.Add(item);
        }

        context.Items.AddRange(items);
        await context.SaveChangesAsync();

        _logger.Information("Seeded {Count} new items (Total: {Total})", items.Count, existingItemCount + items.Count);

        // Log statistics
        var categoryCounts = items.GroupBy(i => i.Category)
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();
        _logger.Information("Items by category: {Stats}", string.Join(", ", categoryCounts));
    }
}
