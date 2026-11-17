using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence.Security.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.DataBase;

public static class DbInitializer
{
    public static async Task SeedAllAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetService<ILoggerFactory>()?.CreateLogger("ACHInterbank.DbInitializer");
        var dbContext = provider.GetService<AchDbContext>();

        try
        {
            if (dbContext is not null)
            {
                dbContext.AuditEnabled = false;
                try
                {
                    logger?.LogInformation("Seed bootstrap starting: SOAP catalog and mapping reference sets");
                    await new Cfa.ACHInterbank.Persistence.Integrations.Services.IntegrationMappingBootstrapper(dbContext).EnsureAsync();
                    logger?.LogInformation("Seed bootstrap completed: SOAP catalog and mapping reference sets");
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Seed bootstrap failed: SOAP catalog and mapping reference sets");
                    throw;
                }

                dbContext.AuditEnabled = true;
                try
                {
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    logger?.LogInformation("Seed bootstrap starting: SOAP transport settings");
                    await new SoapIntegrationSettingsBootstrapper(dbContext, configuration).EnsureAsync();
                    logger?.LogInformation("Seed bootstrap completed: SOAP transport settings");
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Seed bootstrap failed: SOAP transport settings");
                    throw;
                }
                finally
                {
                    dbContext.AuditEnabled = false;
                }
            }

            var seeders = DbSeederExecutionOrder.Apply(provider.GetServices<IDbSeeder>());
            foreach (var seeder in seeders)
            {
                var seederName = seeder.GetType().FullName ?? seeder.GetType().Name;
                logger?.LogInformation("Seed starting: {Seeder} (order {Order})", seederName, seeder.Order);
                try
                {
                    await seeder.SeedAsync();
                    logger?.LogInformation("Seed completed: {Seeder}", seederName);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Seed failed: {Seeder} (order {Order})", seederName, seeder.Order);
                    throw;
                }
            }
        }
        finally
        {
            if (dbContext is not null)
            {
                dbContext.AuditEnabled = true;
            }
        }
    }
}
