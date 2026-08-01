using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence.Security.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Persistence.DataBase;

public static class DbInitializer
{
    public static async Task SeedAllAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        
        var dbContext = scope.ServiceProvider.GetService<AchDbContext>();
        if (dbContext is not null)
        {
            dbContext.AuditEnabled = false;
            await new Cfa.ACHInterbank.Persistence.Integrations.Services.IntegrationCatalogBootstrapper(dbContext).EnsureAsync();
            await new Cfa.ACHInterbank.Persistence.Integrations.Services.IntegrationMappingBootstrapper(dbContext).EnsureAsync();

            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            dbContext.AuditEnabled = true;
            await new SoapIntegrationSettingsBootstrapper(dbContext, configuration).EnsureAsync();
            dbContext.AuditEnabled = false;
        }

        var seeders = scope.ServiceProvider
                           .GetServices<IDbSeeder>()
                           .OrderBy(s => s.Order);

        try
        {
            foreach (var seeder in seeders)
            {
                await seeder.SeedAsync();
            }
        }
        finally
        {
            if (dbContext is not null)
            {
                dbContext.AuditEnabled = true;
            }
        }

        //await Task.WhenAll(seeders.Select(s => s.SeedAsync()));
    }
}
