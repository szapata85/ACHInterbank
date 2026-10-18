using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav21SeedInfrastructureTests
{
    [Fact]
    public async Task DbInitializer_UsesStableTypeNameAsTieBreakerForEqualOrders()
    {
        var executionLog = new List<string>();
        var services = new ServiceCollection();
        services.AddScoped<IDbSeeder>(_ => new ZetaSameOrderSeeder(executionLog));
        services.AddScoped<IDbSeeder>(_ => new EarlierOrderSeeder(executionLog));
        services.AddScoped<IDbSeeder>(_ => new AlphaSameOrderSeeder(executionLog));

        await using var provider = services.BuildServiceProvider();

        await DbInitializer.SeedAllAsync(provider);

        Assert.Equal(
            new[]
            {
                nameof(EarlierOrderSeeder),
                nameof(AlphaSameOrderSeeder),
                nameof(ZetaSameOrderSeeder)
            },
            executionLog);
    }

    [Fact]
    public void CanonicalNavigationSeeder_IsScheduledAfterCurrentFunctionalNavigationSeeders()
    {
        IDbSeeder[] currentFunctionalNavigationSeeders =
        [
            new NachaSecurityMenuSeeder(null!),
            new NachaConfigMenuSeeder(null!),
            new OperationalRestrictionsMenuSeeder(null!),
            new UsersMenuSeeder(null!),
            new CyclesMenuSeeder(null!),
            new IncomingNachaCommandCenterMenuSeeder(null!),
            new AchClaimsMenuSeeder(null!),
            new AchColombiaFileExchangeMenuSeeder(null!),
            new AchColombiaMftAdministrationMenuSeeder(null!),
            new OutgoingTransactionMonitoringSeeder(null!),
            new ReportsMenuSeeder(null!)
        ];
        var canonical = new NavigationInformationArchitectureSeeder(null!);

        var ordered = DbSeederExecutionOrder.Apply(currentFunctionalNavigationSeeders.Append(canonical)).ToArray();

        Assert.Same(canonical, ordered[^1]);
        Assert.All(currentFunctionalNavigationSeeders, seeder =>
            Assert.True(seeder.Order < canonical.Order, $"{seeder.GetType().Name} must execute before the canonical navigation reconciler."));
    }

    private sealed class EarlierOrderSeeder(List<string> executionLog) : IDbSeeder
    {
        public int Order => 0;

        public Task SeedAsync()
        {
            executionLog.Add(nameof(EarlierOrderSeeder));
            return Task.CompletedTask;
        }
    }

    private sealed class AlphaSameOrderSeeder(List<string> executionLog) : IDbSeeder
    {
        public int Order => 5;

        public Task SeedAsync()
        {
            executionLog.Add(nameof(AlphaSameOrderSeeder));
            return Task.CompletedTask;
        }
    }

    private sealed class ZetaSameOrderSeeder(List<string> executionLog) : IDbSeeder
    {
        public int Order => 5;

        public Task SeedAsync()
        {
            executionLog.Add(nameof(ZetaSameOrderSeeder));
            return Task.CompletedTask;
        }
    }
}
