using Backend.Features.Items.Enums;
using Backend.Persistence;
using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Backend.Data;

public static class DatabaseSeeder
{
    private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(DatabaseSeeder));

    public static async Task SeedAsync(
        ApplicationContext context,
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        try
        {
            Logger.Information("Starting database seeding...");

            // 1. Seed Roles
            await SeedRoles(roleManager);

            // 2. Seed Universities
            var universities = await SeedUniversities(context);

            // 3. Seed Admin Account (fixed admin user)
            await SeedAdminAccount(userManager, universities);

            // 4. Seed Users (Moderators and regular Users only)
            var users = await SeedUsers(context, userManager, universities);

            // 5. Seed Items (100 items)
            await SeedItems(context, users);

            Logger.Information("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Fatal error: Database seeding failed.", ex);
        }
    }

    private static async Task SeedRoles(RoleManager<IdentityRole<Guid>> roleManager)
    {
        Logger.Information("Seeding roles...");

        var roles = new[] { "Admin", "Moderator", "User", "Seller" };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole<Guid>(roleName);
                await roleManager.CreateAsync(role);
                Logger.Information("Created role: {RoleName}", roleName);
            }
            else
            {
                Logger.Information("Role already exists: {RoleName}", roleName);
            }
        }
    }

    private static async Task<List<University>> SeedUniversities(ApplicationContext context)
    {
        Logger.Information("Seeding universities...");

        if (await context.Universities.AnyAsync())
        {
            Logger.Information("Universities already exist, skipping seeding");
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

        Logger.Information("Seeded {Count} universities", universities.Count);
        return universities;
    }

    private static async Task SeedAdminAccount(
        UserManager<User> userManager,
        List<University> universities)
    {
        Logger.Information("Seeding admin account...");

        const string adminEmail = "admin@student.uaic.ro";
        const string adminPassword = "Admin@1234";
        const string adminFirstName = "Admin";
        const string adminLastName = "UniShare";

        // Check if admin already exists
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin != null)
        {
            Logger.Information("Admin account already exists: {Email}", adminEmail);
            return;
        }

        // Get UAIC university for admin (or first university as fallback)
        var adminUniversity = universities.FirstOrDefault(u => u.ShortCode == "UAIC")
                              ?? universities[0];

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = adminFirstName,
            LastName = adminLastName,
            Email = adminEmail,
            UserName = adminEmail,
            NormalizedEmail = adminEmail.ToUpper(),
            NormalizedUserName = adminEmail.ToUpper(),
            NewEmailConfirmed = true, // Admin is always verified
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

            Logger.Information("✅ Created admin account: {Email} with password: {Password}",
                adminEmail, adminPassword);
        }
        else
        {
            Logger.Error("Failed to create admin account: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task<List<User>> SeedUsers(
        ApplicationContext context,
        UserManager<User> userManager,
        List<University> universities)
    {
        Logger.Information("Seeding users (Moderators and regular Users)...");
        const int targetUserCount = 10;

        // 1. Extract Filtering Logic
        var nonAdminUsers = await GetExistingNonAdminUsers(context, userManager);
        var existingUserCount = nonAdminUsers.Count;

        if (existingUserCount >= targetUserCount)
        {
            Logger.Information("Non-admin user count ({ExistingCount}) meets target ({TargetCount}).",
                existingUserCount, targetUserCount);
            return await context.Users.ToListAsync();
        }

        var usersToCreate = targetUserCount - existingUserCount;
        var newUsers = new List<User>();
        var random = new Random(12345 + existingUserCount);

        // 2. Setup Faker once
        var userFaker = CreateUserFaker();

        // 3. Check Moderator status
        // Note: Use await here properly instead of .Result to avoid deadlocks
        var hasModerator = false;
        foreach (var u in nonAdminUsers)
        {
            var roles = await userManager.GetRolesAsync(u);
            if (roles.Contains("Moderator"))
            {
                hasModerator = true;
                break;
            }
        }

        // 4. Simplified Main Loop
        for (int i = 0; i < usersToCreate; i++)
        {
            var shouldBeModerator = !hasModerator && i == 0;

            var newUser = await CreateSingleUserAsync(
                userManager,
                universities,
                userFaker,
                random,
                shouldBeModerator
            );

            if (newUser != null)
            {
                newUsers.Add(newUser);
                if (shouldBeModerator) hasModerator = true;
            }
        }

        Logger.Information("Seeded {Count} new users.", newUsers.Count);
        return await context.Users.ToListAsync();
    }

// Helper 1: User Filtering
    private static async Task<List<User>> GetExistingNonAdminUsers(
        ApplicationContext context,
        UserManager<User> userManager)
    {
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

        return nonAdminUsers;
    }

// Helper 2: Encapsulate Faker Configuration
    private static Faker<User> CreateUserFaker()
    {
        return new Faker<User>()
            .RuleFor(u => u.Id, f => Guid.NewGuid())
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.CreatedAt, f => f.Date.Past(1, DateTime.UtcNow))
            .RuleFor(u => u.NewEmailConfirmed, f => f.Random.Bool(0.7f))
            .RuleFor(u => u.PhoneNumberConfirmed, f => false)
            .RuleFor(u => u.TwoFactorEnabled, f => false)
            .RuleFor(u => u.LockoutEnabled, f => false);
    }

// Helper 3: Complex Email Logic
    private static string GenerateUniversityEmail(
        User user,
        University university,
        Random random)
    {
        var emailPrefix = $"{user.FirstName.ToLower()}.{user.LastName.ToLower()}{random.Next(1, 999)}";
        var isStudent = random.Next(0, 2) == 0;

        if (isStudent && university.EmailDomain.Contains('@'))
        {
            var domain = university.EmailDomain.Substring(1);
            return $"{emailPrefix}@student.{domain}";
        }

        return $"{emailPrefix}{university.EmailDomain}";
    }

// Helper 4: Logic for creating a single user and assigning roles
    private static async Task<User?> CreateSingleUserAsync(
        UserManager<User> userManager,
        List<University> universities,
        Faker<User> userFaker,
        Random random,
        bool shouldBeModerator)
    {
        var university = universities[random.Next(universities.Count)];
        var user = userFaker.Generate();

        user.UniversityId = university.Id;
        user.Email = GenerateUniversityEmail(user, university, random);
        user.UserName = user.Email;
        user.NormalizedEmail = user.Email.ToUpper();
        user.NormalizedUserName = user.Email.ToUpper();

        var result = await userManager.CreateAsync(user, "Test@1234");

        if (!result.Succeeded)
        {
            Logger.Error("Failed to create user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return null;
        }

        await userManager.AddToRoleAsync(user, "User");

        if (shouldBeModerator)
        {
            await userManager.AddToRoleAsync(user, "Moderator");
            Logger.Information("Created moderator user: {Email}", user.Email);
        }
        else
        {
            Logger.Information("Created regular user: {Email}", user.Email);
        }

        return user;
    }

    private static async Task SeedItems(ApplicationContext context, List<User> users)
    {
        Logger.Information("Seeding items...");

        const int targetItemCount = 100;
        var existingItemCount = await context.Items.CountAsync();

        if (existingItemCount >= targetItemCount)
        {
            Logger.Information(
                "Item count ({ExistingCount}) meets or exceeds target ({TargetCount}), skipping item seeding",
                existingItemCount, targetItemCount);
            return;
        }

        var itemsToCreate = targetItemCount - existingItemCount;
        Logger.Information("Found {ExistingCount} items, creating {ToCreate} more to reach target of {TargetCount}",
            existingItemCount, itemsToCreate, targetItemCount);

        var random = new Random(12345 + existingItemCount); // Different seed based on existing count
        Randomizer.Seed = new Random(12345 + existingItemCount);

        // Item name templates by category
        var bookTitles = new[]
        {
            "Introduction to Programming", "Data Structures and Algorithms",
            "Calculus I", "Physics Fundamentals", "Chemistry Basics", "English Literature",
            "Modern History", "Psychology 101", "Biology Textbook", "Economics Principles"
        };

        var electronicItems = new[]
        {
            "USB-C Cable", "Wireless Mouse", "Keyboard", "Headphones",
            "Laptop Charger", "Phone Holder", "HDMI Cable", "USB Hub", "Webcam", "External Hard Drive"
        };

        var kitchenItems = new[]
        {
            "Coffee Mug", "Water Bottle", "Lunch Box", "Food Container",
            "Cutlery Set", "Plate", "Bowl", "Frying Pan", "Pot", "Kitchen Knife"
        };

        var clothingItems = new[]
        {
            "T-Shirt", "Hoodie", "Jeans", "Jacket", "Sweater",
            "Scarf", "Hat", "Gloves", "Socks", "Sports Shoes"
        };

        var accessories = new[]
        {
            "Backpack", "Laptop Bag", "Umbrella", "Sunglasses",
            "Watch", "Wallet", "Pen Set", "Notebook", "Calculator", "Water Flask"
        };

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
                ItemCategory.Clothing => clothingItems[random.Next(clothingItems.Length)] +
                                         $" Size {new[] { "S", "M", "L", "XL" }[random.Next(4)]}",
                ItemCategory.Accessories => accessories[random.Next(accessories.Length)],
                _ => $"Miscellaneous Item {existingItemCount + i + 1}"
            };

            // Generate description
            var faker = new Faker();
            item.Description = category switch
            {
                ItemCategory.Books =>
                    $"Academic textbook in {new[] { "good", "excellent", "fair" }[random.Next(3)]} condition. {faker.Lorem.Sentence(10)}",
                ItemCategory.Electronics => $"Electronic device in working condition. {faker.Lorem.Sentence(8)}",
                ItemCategory.Kitchen =>
                    $"Kitchen item, {new[] { "lightly used", "like new", "well-maintained" }[random.Next(3)]}. {faker.Lorem.Sentence(7)}",
                ItemCategory.Clothing =>
                    $"Clothing item, {new[] { "barely worn", "gently used", "in good shape" }[random.Next(3)]}. {faker.Lorem.Sentence(6)}",
                ItemCategory.Accessories => $"Useful accessory for daily use. {faker.Lorem.Sentence(8)}",
                _ => faker.Lorem.Sentences(2)
            };

            // Assign condition
            item.Condition = (ItemCondition)random.Next(0, 5);

            items.Add(item);
        }

        context.Items.AddRange(items);
        await context.SaveChangesAsync();

        Logger.Information("Seeded {Count} new items (Total: {Total})", items.Count, existingItemCount + items.Count);

        // Log statistics
        var categoryCounts = items.GroupBy(i => i.Category)
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();
        Logger.Information("Items by category: {Stats}", string.Join(", ", categoryCounts));
    }
}