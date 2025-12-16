using Backend.Features.Items.Enums;
using Backend.Features.Reports.Enums;
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

            // 4. Seed Moderator Accounts (2-3 moderators)
            var moderators = await SeedModeratorAccounts(context, userManager, universities);

            // 5. Seed Users (regular Users only)
            var users = await SeedUsers(context, userManager, universities);

            // 6. Seed Items (100 items)
            var items = await SeedItems(context, users);

            // 7. Seed Reports (with custom descriptions)
            await SeedReports(context, users, items, moderators);

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
                EmailDomain = "@uaic.ro",
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

    private static async Task<List<User>> SeedModeratorAccounts(
        ApplicationContext context,
        UserManager<User> userManager,
        List<University> universities)
    {
        _logger.Information("Seeding moderator accounts...");

        const int targetModeratorCount = 5; // Ensure at least 5 moderators
        
        // Count existing moderators (exclude admin)
        var allUsers = await context.Users.ToListAsync();
        var existingModerators = new List<User>();
        
        foreach (var user in allUsers)
        {
            var roles = await userManager.GetRolesAsync(user);
            if (roles.Contains("Moderator") && !roles.Contains("Admin"))
            {
                existingModerators.Add(user);
            }
        }

        var existingModeratorCount = existingModerators.Count;

        if (existingModeratorCount >= targetModeratorCount)
        {
            _logger.Information("Moderator count ({ExistingCount}) meets or exceeds target ({TargetCount}), skipping moderator seeding", 
                existingModeratorCount, targetModeratorCount);
            return existingModerators;
        }

        var moderatorsToCreate = targetModeratorCount - existingModeratorCount;
        _logger.Information("Found {ExistingCount} existing moderators, creating {ToCreate} more to reach target of {TargetCount}", 
            existingModeratorCount, moderatorsToCreate, targetModeratorCount);

        var moderators = new List<User>();

        // Predefined moderator accounts with specific names
        var moderatorData = new[]
        {
            new { FirstName = "Maria", LastName = "Moderator", University = "UAIC" },
            new { FirstName = "Alex", LastName = "Moderator", University = "TUIASI" },
            new { FirstName = "Elena", LastName = "Moderator", University = "UPB" },
            new { FirstName = "Andrei", LastName = "Moderator", University = "UBB" },
            new { FirstName = "Diana", LastName = "Moderator", University = "UB" }
        };

        for (int i = 0; i < moderatorsToCreate && i < moderatorData.Length; i++)
        {
            var data = moderatorData[i];
            var university = universities.FirstOrDefault(u => u.ShortCode == data.University) 
                             ?? universities.First();

            var moderator = new User
            {
                Id = Guid.NewGuid(),
                FirstName = data.FirstName,
                LastName = data.LastName,
                Email = $"{data.FirstName.ToLower()}.{data.LastName.ToLower()}{university.EmailDomain}",
                UserName = $"{data.FirstName.ToLower()}.{data.LastName.ToLower()}{university.EmailDomain}",
                EmailConfirmed = true, // Moderators should have verified email
                UniversityId = university.Id,
                CreatedAt = DateTime.UtcNow,
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = false
            };

            moderator.NormalizedEmail = moderator.Email.ToUpper();
            moderator.NormalizedUserName = moderator.UserName.ToUpper();

            var password = "Moderator@123";
            var result = await userManager.CreateAsync(moderator, password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(moderator, "User");
                await userManager.AddToRoleAsync(moderator, "Moderator");

                _logger.Information("✅ Created moderator account: {Email} (Password: {Password})", 
                    moderator.Email, password);

                moderators.Add(moderator);
            }
            else
            {
                _logger.Error("Failed to create moderator account: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        _logger.Information("Seeded {Count} new moderators (Total: {Total})", 
            moderators.Count, existingModeratorCount + moderators.Count);

        return existingModerators.Concat(moderators).ToList();
    }

    private static async Task<List<Item>> SeedItems(ApplicationContext context, List<User> users)
    {
        _logger.Information("Seeding items...");

        const int targetItemCount = 100;
        var existingItemCount = await context.Items.CountAsync();

        if (existingItemCount >= targetItemCount)
        {
            _logger.Information("Item count ({ExistingCount}) meets or exceeds target ({TargetCount}), skipping item seeding", 
                existingItemCount, targetItemCount);
            return await context.Items.ToListAsync();
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

        // Return the items list at the end
        return await context.Items.ToListAsync();
    }

    private static async Task SeedReports(
        ApplicationContext context,
        List<User> users,
        List<Item> items,
        List<User> moderators)
    {
        _logger.Information("Seeding reports...");

        const int targetReportCount = 30;
        var existingReportCount = await context.Reports.CountAsync();

        if (existingReportCount >= targetReportCount)
        {
            _logger.Information("Report count ({ExistingCount}) meets or exceeds target ({TargetCount}), skipping report seeding", 
                existingReportCount, targetReportCount);
            return;
        }

        var reportsToCreate = targetReportCount - existingReportCount;
        _logger.Information("Found {ExistingCount} existing reports, creating {ToCreate} more to reach target of {TargetCount}", 
            existingReportCount, reportsToCreate, targetReportCount);

        var reports = new List<Report>();
        var random = new Random(67890);

        // Custom report descriptions for different scenarios
        var reportDescriptions = new[]
        {
            "This item description does not match the actual product. The seller claims it's brand new but the images show clear signs of wear.",
            "The price listed for this item seems suspiciously low compared to market value. Possible scam attempt.",
            "Inappropriate content in the item description. Contains offensive language that violates community guidelines.",
            "Seller is not responding to messages. Item was supposed to be available but no communication from owner.",
            "Item condition was misrepresented. Listed as 'Excellent' but received in 'Poor' condition.",
            "Duplicate listing - this same item has been posted multiple times by the same user.",
            "Images do not match the description provided. Appears to be stock photos rather than actual item.",
            "Suspected counterfeit product. Brand name items being sold at unrealistic prices.",
            "Item was already sold but listing is still marked as available. Seller not updating status.",
            "Description contains prohibited items or services according to platform policies.",
            "Seller demanding payment outside the platform which violates our terms of service.",
            "Item location information is incorrect or misleading.",
            "Product safety concern - item appears to be damaged and potentially hazardous.",
            "Listing contains personal contact information which should not be publicly shared.",
            "Discriminatory language used in item description.",
            "False advertising - item specifications do not match what's being delivered.",
            "Seller has multiple negative reports from other users for similar issues.",
            "Item categorization is incorrect, making it difficult for users to find legitimate items.",
            "Spam listing - same item posted repeatedly in short time period.",
            "Misleading title - item name does not reflect actual product being sold."
        };

        // Filter out users who own items (to avoid self-reporting)
        var regularUsers = users.Where(u => !items.Any(i => i.OwnerId == u.Id)).ToList();
        if (!regularUsers.Any())
        {
            regularUsers = users; // Fallback if all users own items
        }

        for (int i = 0; i < reportsToCreate; i++)
        {
            // Select a random item
            var item = items[random.Next(items.Count)];
            
            // Select a random user (who doesn't own the item if possible)
            var reportingUsers = regularUsers.Where(u => u.Id != item.OwnerId).ToList();
            if (!reportingUsers.Any())
            {
                reportingUsers = users.Where(u => u.Id != item.OwnerId).ToList();
            }
            
            var reportingUser = reportingUsers.Any() 
                ? reportingUsers[random.Next(reportingUsers.Count)]
                : users[random.Next(users.Count)];

            var report = new Report
            {
                Id = Guid.NewGuid(),
                ItemId = item.Id,
                OwnerId = item.OwnerId,
                UserId = reportingUser.Id,
                Description = reportDescriptions[i % reportDescriptions.Length],
                CreatedDate = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
                Status = ReportStatus.PENDING
            };

            // Randomly assign some reports as reviewed (30% chance)
            if (random.Next(0, 100) < 30 && moderators.Any())
            {
                var moderator = moderators[random.Next(moderators.Count)];
                report.ModeratorId = moderator.Id;
                
                // Randomly assign status
                var statusRoll = random.Next(0, 100);
                if (statusRoll < 50)
                {
                    report.Status = ReportStatus.ACCEPTED;
                }
                else if (statusRoll < 80)
                {
                    report.Status = ReportStatus.DECLINED;
                }
                // else remains PENDING
            }

            reports.Add(report);
        }

        context.Reports.AddRange(reports);
        await context.SaveChangesAsync();

        _logger.Information("Seeded {Count} new reports (Total: {Total})", 
            reports.Count, existingReportCount + reports.Count);
        
        // Log statistics
        var statusCounts = reports.GroupBy(r => r.Status)
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();
        _logger.Information("Reports by status: {Stats}", string.Join(", ", statusCounts));
    }
}
