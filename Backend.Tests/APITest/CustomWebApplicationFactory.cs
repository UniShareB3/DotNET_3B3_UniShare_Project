using Backend.Data;
using Backend.Persistence;
using Backend.Tests.Seeder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Backend.Tests.APITest;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    
    public CustomWebApplicationFactory()
    {
    }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        Environment.SetEnvironmentVariable("API_FRONTEND_URL", "http://localhost:50147");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ApplicationContext>();
            
            services.RemoveAll<DbContextOptions<ApplicationContext>>();
            
            services.RemoveAll<DbContextOptions>();
            
            services.AddDbContext<ApplicationContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
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
        
        if (!await context.Users.AnyAsync())
        {
            await TestDataSeeder.SeedTestDataAsync(context, userManager, roleManager);
        }
    }

    public async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }
}