using Cfa.ACHInterbank.Application.DataBase;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Persistence.DataBase;

public static class DbInitializer
{
    public static async Task SeedAllAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        
        var seeders = scope.ServiceProvider
                           .GetServices<IDbSeeder>()
                           .OrderBy(s => s.Order);

        foreach (var seeder in seeders)
        {
            await seeder.SeedAsync();
        }

        //await Task.WhenAll(seeders.Select(s => s.SeedAsync()));
    }
}

