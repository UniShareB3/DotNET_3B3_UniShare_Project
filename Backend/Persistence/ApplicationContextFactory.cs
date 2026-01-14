using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Backend.Persistence;

public class ApplicationContextFactory : IDesignTimeDbContextFactory<ApplicationContext>
{
    public ApplicationContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
        
        var connectionString = $"Host={Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost"};" +
                               $"Database={Environment.GetEnvironmentVariable("DB_NAME") ?? "UniShare"};" +
                               $"Username={Environment.GetEnvironmentVariable("DB_USERNAME") ?? "postgres"};" +
                               $"Password={Environment.GetEnvironmentVariable("DB_PASS") ?? "admin"}";
        
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationContext(optionsBuilder.Options);
    }
}

