using System.Data.Common;
using Backend.Data;
using Backend.Persistence;
using Backend.Tests.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Backend.Tests.APITest;

// 1. Implement IAsyncLifetime to handle async seeding safely
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // 1. Remove the Context itself
            services.RemoveAll<ApplicationContext>();
        
            // 2. Remove the generic options (the one you already had)
            services.RemoveAll<DbContextOptions<ApplicationContext>>();
        
            // 3. Remove the non-generic DbContextOptions
            services.RemoveAll<DbContextOptions>();

            // 4. Add the Test Database
            services.AddDbContext<ApplicationContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
                // Optional: Ignore the transaction warning for InMemory
                options.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        var context = scopedServices.GetRequiredService<ApplicationContext>();
        var userManager = scopedServices.GetRequiredService<UserManager<User>>();
        var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await context.Database.EnsureCreatedAsync();
        
        // Ensure we don't double-seed if tests run in parallel sharing the factory
        if (!context.Users.Any())
        {
            await TestDataSeeder.SeedTestDataAsync(context, userManager, roleManager);
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}