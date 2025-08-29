using Cfa.ACHInterbank.Application.DataBase;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Persistence.DataBase;

public static class DbInitializer
{
    public static async Task SeedAllAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var seeders = scope.ServiceProvider.GetServices<IDbSeeder>();

        foreach (var seeder in seeders)
        {
            await seeder.SeedAsync();
        }
    }
}

