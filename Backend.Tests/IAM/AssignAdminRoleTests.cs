using Backend.Data;
using Backend.Features.Shared.IAM.AssignAdminRole;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.IAM.AssignAdminRole;

public class AssignAdminRoleTests
{
    // Static test IDs for reproducibility
    private static readonly Guid TestAdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TestNormalUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TestNonExistentUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    
    private static (UserManager<User>, RoleManager<IdentityRole<Guid>>, ApplicationContext) CreateManagers(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new ApplicationContext(options);
        
        var userStore = new UserStore<User, IdentityRole<Guid>, ApplicationContext, Guid>(context);
        var userManager = new UserManager<User>(
            userStore,
            null,
            new PasswordHasher<User>(),
            null,
            null,
            null,
            null,
            null,
            null);
        
        var roleStore = new RoleStore<IdentityRole<Guid>, ApplicationContext, Guid>(context);
        var roleManager = new RoleManager<IdentityRole<Guid>>(
            roleStore,
            null,
            null,
            null,
            null);
            
        return (userManager, roleManager, context);
    }

    [Fact]
    public async Task When_AssignAdminRole_AndAssignerIsAdmin_Then_RoleIsAssigned()
    {
        // Arrange
        var (userManager, roleManager, context) = CreateManagers($"AssignAdminRole_{Guid.NewGuid()}");
        
        // Create Admin role
        await roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));
        
        var adminUser = new User
        {
            Id = TestAdminUserId,
            UserName = "adminuser",
            Email = "admin@uaic.ro"
        };
        
        await userManager.CreateAsync(adminUser);
        await userManager.AddToRoleAsync(adminUser, "Admin");
        
        var normalUser = new User
        {
            Id = TestNormalUserId,
            UserName = "normaluser",
            Email = "normaluser@uaic.ro"
        };
        await userManager.CreateAsync(normalUser);
        
        AssignAdminRoleRequest adminRoleRequest = new AssignAdminRoleRequest(normalUser.Id);
        AssignAdminRoleHandler handler = new AssignAdminRoleHandler(userManager);

        // Act
        var result = await handler.Handle(adminRoleRequest, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        Assert.NotNull(result);
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        var isInRole = await userManager.IsInRoleAsync(normalUser, "Admin");
        isInRole.Should().BeTrue();
        
        // Cleanup
        context.Dispose();
    }
    
    [Fact]
    public async Task When_AssignAdminRole_AndUserAlreadyAdmin_Then_ReturnsBadRequest()
    {
        // Arrange
        var (userManager, roleManager, context) = CreateManagers($"AssignAdminRole_{Guid.NewGuid()}");
        
        // Create Admin role
        await roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));
        
        var adminUser = new User
        {
            Id = TestAdminUserId,
            UserName = "adminuser",
            Email = "admin@uaic.ro"
        };
        
        await userManager.CreateAsync(adminUser);
        await userManager.AddToRoleAsync(adminUser, "Admin");
        
        AssignAdminRoleRequest adminRoleRequest = new AssignAdminRoleRequest(adminUser.Id);
        AssignAdminRoleHandler handler = new AssignAdminRoleHandler(userManager);

        // Act
        var result = await handler.Handle(adminRoleRequest, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        Assert.NotNull(result);
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        
        // Cleanup
        context.Dispose();
    }
    
    [Fact]
    public async Task When_AssignAdminRole_AndUserNotFound_Then_ReturnsNotFound()
    {
        // Arrange
        var (userManager, roleManager, context) = CreateManagers($"AssignAdminRole_{Guid.NewGuid()}");
        
        // Create Admin role
        await roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));
        
        AssignAdminRoleRequest adminRoleRequest = new AssignAdminRoleRequest(TestNonExistentUserId);
        AssignAdminRoleHandler handler = new AssignAdminRoleHandler(userManager);

        // Act
        var result = await handler.Handle(adminRoleRequest, CancellationToken.None) as IStatusCodeHttpResult;

        // Assert
        Assert.NotNull(result);
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        
        // Cleanup
        context.Dispose();
    }
}