using Backend.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace Backend.Tests.APITest;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// Configures the test server with an in-memory database and other test-specific services.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    /// <summary>
    /// Configures the web host for testing by:
    /// - Using an in-memory database instead of the production database
    /// - Clearing existing database registrations
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ApplicationContext>();
            services.RemoveAll<DbContextOptions<ApplicationContext>>();
            services.RemoveAll<IDbContextFactory<ApplicationContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationContext>>();

            services.AddDbContext<ApplicationContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

        });
    }
}