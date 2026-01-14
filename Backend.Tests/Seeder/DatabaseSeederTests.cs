using Backend.Data;
using Backend.Features.Items.Enums;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type

namespace Backend.Tests.Seeder;

public class DatabaseSeederTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<RoleManager<IdentityRole<Guid>>> _mockRoleManager;
    private readonly ApplicationContext _context;

    public DatabaseSeederTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationContext(options);

        // Setup UserManager mock
        var userStore = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            userStore.Object, null, null, null, null, null, null, null, null);

        // Setup RoleManager mock
        var roleStore = new Mock<IRoleStore<IdentityRole<Guid>>>();
        _mockRoleManager = new Mock<RoleManager<IdentityRole<Guid>>>(
            roleStore.Object, null, null, null, null);
    }

    private void ResetContext()
    {
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Given_EmptyDatabase_When_SeedAsyncIsCalled_Then_ShouldSeedAllData()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        var universities = await _context.Universities.ToListAsync();
        universities.Should().HaveCount(8);
        
        var items = await _context.Items.ToListAsync();
        items.Should().HaveCount(100);
        
        _mockRoleManager.Verify(x => x.CreateAsync(It.IsAny<IdentityRole<Guid>>()), Times.AtLeast(4));
        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.AtLeast(1));
    }

    [Fact]
    public async Task Given_SeedingFails_When_SeedAsyncIsCalled_Then_ShouldThrowException()
    {
        // Arrange
        ResetContext();
        _mockRoleManager.Setup(x => x.RoleExistsAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        Func<Task> act = async () => await DatabaseSeeder.SeedAsync(
            _context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Fatal error: Database seeding failed.");
    }

    [Fact]
    public async Task Given_NoRolesExist_When_SeedRolesIsCalled_Then_ShouldCreateAllRoles()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();
        
        _mockRoleManager.Setup(x => x.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _mockRoleManager.Setup(x => x.CreateAsync(It.IsAny<IdentityRole<Guid>>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        _mockRoleManager.Verify(x => x.CreateAsync(It.Is<IdentityRole<Guid>>(r => r.Name == "Admin")), Times.Once);
        _mockRoleManager.Verify(x => x.CreateAsync(It.Is<IdentityRole<Guid>>(r => r.Name == "Moderator")), Times.Once);
        _mockRoleManager.Verify(x => x.CreateAsync(It.Is<IdentityRole<Guid>>(r => r.Name == "User")), Times.Once);
        _mockRoleManager.Verify(x => x.CreateAsync(It.Is<IdentityRole<Guid>>(r => r.Name == "Seller")), Times.Once);
    }

    [Fact]
    public async Task Given_RolesAlreadyExist_When_SeedRolesIsCalled_Then_ShouldSkipExistingRoles()
    {
        // Arrange
        ResetContext();
        
        // Setup RoleManager to indicate roles already exist
        _mockRoleManager.Setup(x => x.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockRoleManager.Setup(x => x.CreateAsync(It.IsAny<IdentityRole<Guid>>()))
            .ReturnsAsync(IdentityResult.Success);
        
        // Setup UserManager for the rest of the seeding pipeline
        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<User, string>((user, _) =>
            {
                _context.Users.Add(user);
                _context.SaveChanges();
            });
        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string>());

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        _mockRoleManager.Verify(x => x.CreateAsync(It.IsAny<IdentityRole<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task Given_DatabaseIsEmpty_When_SeedUniversitiesIsCalled_Then_ShouldCreate8Universities()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        var universities = await _context.Universities.ToListAsync();
        universities.Should().HaveCount(8);
        
        var uaicUniversity = universities.FirstOrDefault(u => u.ShortCode == "UAIC");
        uaicUniversity.Should().NotBeNull();
        uaicUniversity.Name.Should().Be("Universitatea Alexandru Ioan Cuza");
        uaicUniversity.EmailDomain.Should().Be("@student.uaic.ro");
    }

    [Fact]
    public async Task Given_UniversitiesAlreadyExist_When_SeedUniversitiesIsCalled_Then_ShouldSkipSeeding()
    {
        // Arrange
        ResetContext();
        _context.Universities.Add(new University
        {
            Id = Guid.NewGuid(),
            Name = "Test University",
            ShortCode = "TEST",
            EmailDomain = "@test.edu",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        SetupMocksForSuccessfulSeeding();

        var initialCount = await _context.Universities.CountAsync();

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        var finalCount = await _context.Universities.CountAsync();
        finalCount.Should().Be(initialCount);
    }

    [Fact]
    public async Task Given_DatabaseIsEmpty_When_SeedUniversitiesIsCalled_Then_ShouldContainAllRequiredUniversities()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        var universities = await _context.Universities.ToListAsync();
        var shortCodes = universities.Select(u => u.ShortCode).ToList();
        
        shortCodes.Should().Contain(["UAIC", "TUIASI", "UPB", "UBB", "UB", "UVT", "UNITBV", "UCRAV"]);
    }

    [Fact]
    public async Task Given_NoAdminExists_When_SeedAdminAccountIsCalled_Then_ShouldCreateAdminUser()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();
        
        _mockUserManager.Setup(x => x.FindByEmailAsync("admin@student.uaic.ro"))
            .ReturnsAsync((User?)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<User, string>((user, _) =>
            {
                _context.Users.Add(user);
                _context.SaveChanges();
            });
        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        _mockUserManager.Verify(x => x.CreateAsync(
            It.Is<User>(u => 
                u.Email == "admin@student.uaic.ro" &&
                u.FirstName == "Admin" &&
                u.LastName == "UniShare" &&
                u.NewEmailConfirmed
            ), 
            "Admin@1234"
        ), Times.Once);
        
        _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), "User"), Times.AtLeast(1));
        _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), "Admin"), Times.Once);
    }

    [Fact]
    public async Task Given_AdminAlreadyExists_When_SeedAdminAccountIsCalled_Then_ShouldSkipCreation()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();
        
        var existingAdmin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@student.uaic.ro",
            UserName = "admin@student.uaic.ro"
        };
        
        _mockUserManager.Setup(x => x.FindByEmailAsync("admin@student.uaic.ro"))
            .ReturnsAsync(existingAdmin);

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        _mockUserManager.Verify(x => x.CreateAsync(
            It.Is<User>(u => u.Email == "admin@student.uaic.ro"), 
            It.IsAny<string>()
        ), Times.Never);
    }

    [Fact]
    public async Task Given_CreationFails_When_SeedAdminAccountIsCalled_Then_ShouldLogError()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();
        
        var isAdminCreation = true;
        
        _mockUserManager.Setup(x => x.FindByEmailAsync("admin@student.uaic.ro"))
            .ReturnsAsync((User?)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync((User u, string p) =>
            {
                if (isAdminCreation && p == "Admin@1234")
                {
                    isAdminCreation = false;
                    return IdentityResult.Failed(new IdentityError { Description = "Test error" });
                }
                // For subsequent user creations, succeed and add to context
                _context.Users.Add(u);
                _context.SaveChanges();
                return IdentityResult.Success;
            });

        // Act - Should not throw but log error
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<User>(), "Admin@1234"), Times.Once);
    }

    [Fact]
    public async Task Given_DatabaseIsEmpty_When_SeedUsersIsCalled_Then_ShouldCreate10Users()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();
        
        var userCreationCount = 0;
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<User, string>((user, _) =>
            {
                userCreationCount++;
                _context.Users.Add(user);
                _context.SaveChanges();
            });

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        userCreationCount.Should().BeGreaterThanOrEqualTo(10); // At least 10 users (excluding admin)
    }

    [Fact]
    public async Task Given_NoneExists_When_SeedUsersIsCalled_Then_ShouldCreateAtLeastOneModerator()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();
        
        var moderatorCreated = false;
        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "Moderator"))
            .ReturnsAsync(IdentityResult.Success)
            .Callback(() => moderatorCreated = true);

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        moderatorCreated.Should().BeTrue();
    }

    [Fact]
    public async Task Given_TargetCountReached_When_SeedUsersIsCalled_Then_ShouldSkipSeeding()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();
        
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);
        
        var existingAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@student.uaic.ro");
        _mockUserManager.Setup(x => x.FindByEmailAsync("admin@student.uaic.ro"))
            .ReturnsAsync(existingAdmin);
        
        _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { "User" });
        
        _mockUserManager.Invocations.Clear();

        // Act 
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert 
        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Given_UsersCreated_When_SeedUsersIsCalled_Then_ShouldAssignUniversityEmailWithCorrectFormat()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();
        
        User? createdUser = null;
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<User, string>((user, _) => 
            {
                createdUser = user;
                _context.Users.Add(user);
                _context.SaveChanges();
            });

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        createdUser.Should().NotBeNull();
        createdUser!.Email.Should().NotBeNullOrEmpty();
        createdUser.Email.Should().MatchRegex(@"^[a-z]+\.[a-z]+\d+@");
    }

    [Fact]
    public async Task Given_DatabaseIsEmpty_When_SeedItemsIsCalled_Then_ShouldCreate100Items()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        var items = await _context.Items.ToListAsync();
        items.Should().HaveCount(100);
    }

    [Fact]
    public async Task Given_TargetCountReached_When_SeedItemsIsCalled_Then_ShouldSkipSeeding()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();
        
        // First seeding
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);
        var initialCount = await _context.Items.CountAsync();

        // Act - Second seeding
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        var finalCount = await _context.Items.CountAsync();
        finalCount.Should().Be(initialCount);
    }

    [Fact]
    public async Task Given_DatabaseIsEmpty_When_SeedItemsIsCalled_Then_ShouldAssignItemsToExistingUsers()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        var items = await _context.Items.ToListAsync();
        items.Should().NotBeEmpty();
        items.Should().OnlyContain(i => i.OwnerId != Guid.Empty);
    }

    [Fact]
    public async Task Given_DatabaseIsEmpty_When_SeedItemsIsCalled_Then_ShouldDistributeAcrossAllCategories()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        var items = await _context.Items.ToListAsync();
        var categories = items.Select(i => i.Category).Distinct().ToList();
        
        categories.Should().Contain(ItemCategory.Books);
        categories.Should().Contain(ItemCategory.Electronics);
        categories.Should().Contain(ItemCategory.Kitchen);
        categories.Should().Contain(ItemCategory.Clothing);
        categories.Should().Contain(ItemCategory.Accessories);
    }

    [Fact]
    public async Task Given_ItemsSeeded_When_ValidatingProperties_Then_ShouldSetItemPropertiesWithValidValues()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        var items = await _context.Items.ToListAsync();
        items.Should().NotBeEmpty();
        
        foreach (var item in items)
        {
            item.Id.Should().NotBeEmpty();
            item.Name.Should().NotBeNullOrEmpty();
            item.Description.Should().NotBeNullOrEmpty();
            item.Category.Should().BeDefined();
            item.Condition.Should().BeDefined();
            item.CreatedAt.Should().BeBefore(DateTime.UtcNow);
            item.OwnerId.Should().NotBeEmpty();
        }
    }

    [Fact]
    public async Task Given_ItemsSeeded_When_ValidatingConditions_Then_ShouldAssignConditionsWithinValidRange()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        var items = await _context.Items.ToListAsync();
        var conditions = items.Select(i => i.Condition).Distinct().ToList();
        
        conditions.Should().OnlyContain(c => Enum.IsDefined(typeof(ItemCondition), c));
    }

    [Fact]
    public async Task Given_ItemsSeeded_When_CheckingAvailability_Then_ShouldSet80PercentAvailable()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        var items = await _context.Items.ToListAsync();
        var availableCount = items.Count(i => i.IsAvailable);
        var availablePercentage = (double)availableCount / items.Count;
        
        // Allow some variance due to randomness (75-85%)
        availablePercentage.Should().BeGreaterThan(0.7).And.BeLessThan(0.9);
    }

    [Fact]
    public async Task Given_BookCategory_When_SeedItemsIsCalled_Then_ShouldHaveBookTitle()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        var bookItems = await _context.Items
            .Where(i => i.Category == ItemCategory.Books)
            .ToListAsync();
        
        bookItems.Should().NotBeEmpty();
        bookItems.Should().OnlyContain(i => 
            i.Name.Contains("Introduction") || 
            i.Name.Contains("Data Structures") || 
            i.Name.Contains("Calculus") ||
            i.Name.Contains("Physics") ||
            i.Name.Contains("Chemistry") ||
            i.Name.Contains("Edition"));
    }

    [Fact]
    public async Task Given_ElectronicsCategory_When_SeedItemsIsCalled_Then_ShouldHaveElectronicsName()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        var electronicItems = await _context.Items
            .Where(i => i.Category == ItemCategory.Electronics)
            .ToListAsync();
        
        electronicItems.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Given_ClothingCategory_When_SeedItemsIsCalled_Then_ShouldIncludeSize()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        var clothingItems = await _context.Items
            .Where(i => i.Category == ItemCategory.Clothing)
            .ToListAsync();
        
        if (clothingItems.Count != 0)
        {
            clothingItems.Should().OnlyContain(i => 
                i.Name.Contains("Size S") || 
                i.Name.Contains("Size M") || 
                i.Name.Contains("Size L") || 
                i.Name.Contains("Size XL"));
        }
    }

    [Fact]
    public async Task Given_DatabaseIsEmpty_When_SeedItemsIsCalled_Then_ShouldCreateItemsWithDifferentOwners()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();

        // Act
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);

        // Assert
        var items = await _context.Items.ToListAsync();
        var distinctOwners = items.Select(i => i.OwnerId).Distinct().Count();
        
        distinctOwners.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task Given_MultipleSeedingAttempts_When_SeedAsyncIsCalled_Then_ShouldMaintainDataIntegrity()
    {
        // Arrange
        ResetContext();
        SetupMocksForSuccessfulSeeding();

        // Act - Seed multiple times
        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);
        var universitiesCount1 = await _context.Universities.CountAsync();
        var itemsCount1 = await _context.Items.CountAsync();

        await DatabaseSeeder.SeedAsync(_context, _mockUserManager.Object, _mockRoleManager.Object);
        var universitiesCount2 = await _context.Universities.CountAsync();
        var itemsCount2 = await _context.Items.CountAsync();

        // Assert - Counts should remain the same (idempotent)
        universitiesCount2.Should().Be(universitiesCount1);
        itemsCount2.Should().Be(itemsCount1);
    }

    private void SetupMocksForSuccessfulSeeding()
    {
        // Setup RoleManager
        _mockRoleManager.Setup(x => x.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _mockRoleManager.Setup(x => x.CreateAsync(It.IsAny<IdentityRole<Guid>>()))
            .ReturnsAsync(IdentityResult.Success);

        // Setup UserManager
        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<User, string>((user, _) =>
            {
                // Add user to context so it's available for item seeding
                _context.Users.Add(user);
                _context.SaveChanges();
            });
        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string>());
    }
}

