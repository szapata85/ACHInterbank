using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// NAV1 characterization tests. These assertions intentionally freeze the current
/// navigation baseline, including known order collisions and the duplicated /ach-cycles
/// parent/child route. NAV2 may change these expectations deliberately.
/// </summary>
public sealed class NavigationMenuNav1BaselineTests
{
    [Fact]
    public async Task CurrentSeeders_ProduceTheCharacterizedRootAndTransactionsOrdering()
    {
        await using var fixture = CreateFixture();

        await SeedCurrentNavigationAsync(fixture.Context);
        await SeedCurrentNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var roots = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == null)
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Route)
            .Select(item => new { item.Route, item.Order })
            .ToListAsync();

        (string Route, int Order)[] expectedRoots =
        [
            ("/dashboard", 1),
            ("/nacha-config-admin", 2),
            ("/users", 2),
            ("/ach-cycles", 4),
            ("/catalogs", 5),
            ("/integraciones", 5),
            ("/transactions", 6),
            ("/navigation/menu-items", 7),
            ("/reports", 7),
            ("/nacha-security", 8),
            ("/scheduler", 9),
            ("/logs", 10),
            ("/uat", 11)
        ];
        Assert.Equal(expectedRoots, roots.Select(item => (item.Route, item.Order)).ToArray());

        var transactionChildren = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == MenuItemConfiguration.TransactionsId)
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Route)
            .Select(item => new { item.Route, item.Order })
            .ToListAsync();

        (string Route, int Order)[] expectedTransactionChildren =
        [
            ("/transactions/create", 1),
            ("/transactions/list", 2),
            ("/transactions/nacha-upload", 3),
            ("/customer-third-parties", 4),
            ("/transactions/returns", 5),
            ("/transactions/outgoing-monitoring", 6),
            ("/incoming-nacha-command-center", 7),
            ("/ach-colombia/file-exchange", 8),
            ("/ach-claims", 9),
            ("/administracion/mft-ach", 9)
        ];
        Assert.Equal(
            expectedTransactionChildren,
            transactionChildren.Select(item => (item.Route, item.Order)).ToArray());
    }

    [Fact]
    public async Task CurrentSeeders_KeepTheKnownSiblingOrderCollisions()
    {
        await using var fixture = CreateFixture();
        await SeedCurrentNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var activeItems = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive)
            .Select(item => new { item.Id, item.ParentId, item.Route, item.Order })
            .ToListAsync();

        var routesById = activeItems.ToDictionary(item => item.Id, item => item.Route);
        var collisions = activeItems
            .GroupBy(item => new { item.ParentId, item.Order })
            .Where(group => group.Count() > 1)
            .Select(group =>
            {
                var parent = group.Key.ParentId is null
                    ? "root"
                    : routesById[group.Key.ParentId.Value];
                var routes = string.Join('|', group.Select(item => item.Route).OrderBy(route => route));
                return $"{parent}:{group.Key.Order}={routes}";
            })
            .OrderBy(value => value)
            .ToArray();

        string[] expectedCollisions =
        [
            "/transactions:9=/ach-claims|/administracion/mft-ach",
            "root:2=/nacha-config-admin|/users",
            "root:5=/catalogs|/integraciones",
            "root:7=/navigation/menu-items|/reports"
        ];
        Assert.Equal(expectedCollisions, collisions);
    }

    [Fact]
    public async Task CurrentCyclesSeeder_KeepsAnActiveParentAndChildWithTheSameRoute()
    {
        await using var fixture = CreateFixture();
        await SeedCurrentNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var cycleEntries = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.Route == "/ach-cycles")
            .OrderBy(item => item.ParentId)
            .Select(item => new { item.Id, item.ParentId, item.Label, item.Order })
            .ToListAsync();

        Assert.Equal(2, cycleEntries.Count);
        Assert.Contains(cycleEntries, item => item.ParentId == null);
        Assert.Contains(cycleEntries, item =>
            item.ParentId == MenuItemConfiguration.AchCyclesId
            && item.Label == "Catálogo de ciclos"
            && item.Order == 1);
    }

    private static async Task SeedCurrentNavigationAsync(AchDbContext context)
    {
        IDbSeeder[] seeders =
        [
            // Permission prerequisites participate in the same global seeder pipeline.
            new ClaimsPermissionSeeder(context),
            new CfaOperationalTransactionRestrictionsPermissionSeeder(context),

            new NachaSecurityMenuSeeder(context),
            new NachaConfigMenuSeeder(context),
            new OperationalRestrictionsMenuSeeder(context),
            new UsersMenuSeeder(context),
            new CyclesMenuSeeder(context),
            new IncomingNachaCommandCenterMenuSeeder(context),
            new AchClaimsMenuSeeder(context),
            new AchColombiaFileExchangeMenuSeeder(context),
            new AchColombiaMftAdministrationMenuSeeder(context),
            new OutgoingTransactionMonitoringSeeder(context),
            new ReportsMenuSeeder(context)
            // NAV1 deliberately stops before the canonical NAV2 reconciler so this test
            // remains a characterization of the functional-seeder baseline.
        ];

        foreach (var seeder in DbSeederExecutionOrder.Apply(seeders))
            await seeder.SeedAsync();
    }

    private static Fixture CreateFixture()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options);
        context.Database.EnsureCreated();

        return new Fixture(context, connection);
    }

    private sealed class Fixture(AchDbContext context, SqliteConnection connection) : IAsyncDisposable
    {
        public AchDbContext Context { get; } = context;

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
