using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Data;
using SajhaSikshya.Data.Seed;

namespace SajhaSikshya.Extensions;

/// <summary>
/// Startup-time pipeline helpers for <see cref="WebApplication"/> that don't belong
/// inline in Program.cs: applying pending EF Core migrations and seeding baseline data.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Applies any pending migrations and seeds roles/default admin. Runs synchronously
    /// at startup, before the app begins accepting requests, using its own DI scope.
    /// </summary>
    public static async Task<WebApplication> InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitialization");

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
            await DbInitializer.SeedAsync(services);
            await CatalogSeeder.SeedAsync(services);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating or seeding the database.");
            throw;
        }

        return app;
    }
}
