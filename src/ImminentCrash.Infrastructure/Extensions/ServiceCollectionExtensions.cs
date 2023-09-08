using ImminentCrash.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, bool enableSensitiveDataLoggin)
    {
        services.AddPooledDbContextFactory<ImminentCrashDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);

                sqlOptions.MigrationsAssembly("ImminentCrash.Infrastructure");

                // Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
            });

            options.EnableSensitiveDataLogging(enableSensitiveDataLoggin);
        });

        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddScoped(o => o.GetRequiredService<IDbContextFactory<ImminentCrashDbContext>>().CreateDbContext());

        return services;
    }
}