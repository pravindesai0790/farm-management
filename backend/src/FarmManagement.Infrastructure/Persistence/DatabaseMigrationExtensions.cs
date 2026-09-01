using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FarmManagement.Infrastructure.Persistence.Seed;

namespace FarmManagement.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    public static async Task ApplyDatabaseMigrationsAsync(
        this IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        logger.LogInformation("Applying pending database migrations.");
        try
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database migration failed.");
            throw;
        }

        logger.LogInformation("Database migrations applied successfully.");

        var seeder = scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
