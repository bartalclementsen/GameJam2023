using ImminentCrash.Infrastructure;
using ImminentCrash.Infrastructure.Seeders;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace Microsoft.Extensions.DependencyInjection;

public static class ApplicationBuilderExtensions
{
    public static async Task<IApplicationBuilder> UseInfrastructureAsync(this IApplicationBuilder app, Func<Task>? testDataRunner = null)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();
        ILogger? logger = scope.ServiceProvider.GetService<ILogger>();

        try
        {
            logger?.LogInformation("Configuring Database");

            using ImminentCrashDbContext context = scope.ServiceProvider
                .GetRequiredService<IDbContextFactory<ImminentCrashDbContext>>()
                .CreateDbContext();

            logger?.LogDebug("Applying pending migrations");
            context.Database.Migrate();

            logger?.LogDebug("Database configured");

            logger?.LogDebug("Seeding database");
            Seeder seeder = new Seeder();
            await seeder.SeedAsync();
            logger?.LogDebug("Database seeded");

            if (testDataRunner != null)
            {
                await testDataRunner.Invoke();
            }

            logger?.LogDebug("Database Configured");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred configuring the database.");
            throw;
        }


        return app;
    }
}
