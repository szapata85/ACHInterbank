using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav25AMonitoringInformationArchitectureTests
{
    private static readonly string[] MonitoringRoutes =
    [
        "/transactions/outgoing-monitoring",
        "/incoming-nacha-command-center",
        "/incoming-nacha-command-center/observability",
        "/incoming-nacha-command-center/queue",
        "/incoming-nacha-command-center/orphan-resolution"
    ];

    [Fact]
    public async Task CanonicalSeeder_BuildsMonitoringAndControlWithExpectedHierarchyAndOrders()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var operationBefore = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.OperationRoute);
        Assert.True(await fixture.Context.MenuItems.AnyAsync(item =>
            item.IsActive
            && item.ParentId == operationBefore.Id
            && item.Route == "/transactions/outgoing-monitoring"));
        Assert.True(await fixture.Context.MenuItems.AnyAsync(item =>
            item.IsActive
            && item.ParentId == operationBefore.Id
            && item.Route == "/incoming-nacha-command-center"));

        foreach (var route in MonitoringRoutes.Skip(2))
            Assert.False(await fixture.Context.MenuItems.AnyAsync(item => item.Route == route));

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var root = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.MonitoringAndControlRoute);

        Assert.Null(root.ParentId);
        Assert.Equal(NavigationInformationArchitectureSeeder.MonitoringAndControlLabel, root.Label);
        Assert.Equal(40, root.Order);
        Assert.False(root.Exact);

        var children = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == root.Id && MonitoringRoutes.Contains(item.Route))
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order })
            .ToListAsync();

        (string Route, string Label, int Order)[] expected =
        [
            ("/transactions/outgoing-monitoring", "Transacciones enviadas", 10),
            ("/incoming-nacha-command-center", "Seguimiento de archivos", 20),
            ("/incoming-nacha-command-center/observability", "Indicadores operativos", 30),
            ("/incoming-nacha-command-center/queue", "Cola de procesamiento", 40),
            ("/incoming-nacha-command-center/orphan-resolution", "Devoluciones sin relación", 50)
        ];

        Assert.Equal(expected, children.Select(item => (item.Route, item.Label, item.Order)).ToArray());
        Assert.Equal(children.Count, children.Select(item => item.Order).Distinct().Count());

        var operation = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.OperationRoute);
        Assert.False(await fixture.Context.MenuItems.AnyAsync(item =>
            item.IsActive
            && item.ParentId == operation.Id
            && (item.Route == "/transactions/outgoing-monitoring"
                || item.Route == "/incoming-nacha-command-center")));

    }

    [Fact]
    public async Task CanonicalSeeder_PreservesExistingAuthorizationAndMirrorsOnlyCurrentIncomingAuthorizationForNewRows()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var outgoingBefore = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == "/transactions/outgoing-monitoring");
        var incomingBefore = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == "/incoming-nacha-command-center");

        var outgoingPermissionsBefore = await PermissionIdsAsync(fixture.Context, outgoingBefore.Id);
        var outgoingRolesBefore = await RoleIdsAsync(fixture.Context, outgoingBefore.Id);
        var incomingPermissionsBefore = await PermissionIdsAsync(fixture.Context, incomingBefore.Id);
        var incomingRolesBefore = await RoleIdsAsync(fixture.Context, incomingBefore.Id);

        var permissionDefinitionsBefore = await fixture.Context.Permissions
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.Name })
            .ToListAsync();
        var roleDefinitionsBefore = await fixture.Context.Roles
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.Name })
            .ToListAsync();
        var roleGrantsBefore = await fixture.Context.RolePermissions
            .AsNoTracking()
            .OrderBy(item => item.RoleId)
            .ThenBy(item => item.PermissionId)
            .Select(item => new { item.RoleId, item.PermissionId })
            .ToListAsync();

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        Assert.Equal(outgoingPermissionsBefore, await PermissionIdsAsync(fixture.Context, outgoingBefore.Id));
        Assert.Equal(outgoingRolesBefore, await RoleIdsAsync(fixture.Context, outgoingBefore.Id));
        Assert.Equal(incomingPermissionsBefore, await PermissionIdsAsync(fixture.Context, incomingBefore.Id));
        Assert.Equal(incomingRolesBefore, await RoleIdsAsync(fixture.Context, incomingBefore.Id));

        foreach (var route in MonitoringRoutes.Skip(2))
        {
            var itemId = await fixture.Context.MenuItems
                .AsNoTracking()
                .Where(item => item.Route == route)
                .Select(item => item.Id)
                .SingleAsync();

            Assert.Equal(incomingPermissionsBefore, await PermissionIdsAsync(fixture.Context, itemId));
            Assert.Equal(incomingRolesBefore, await RoleIdsAsync(fixture.Context, itemId));
        }

        var permissionDefinitionsAfter = await fixture.Context.Permissions
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.Name })
            .ToListAsync();
        var roleDefinitionsAfter = await fixture.Context.Roles
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.Name })
            .ToListAsync();
        var roleGrantsAfter = await fixture.Context.RolePermissions
            .AsNoTracking()
            .OrderBy(item => item.RoleId)
            .ThenBy(item => item.PermissionId)
            .Select(item => new { item.RoleId, item.PermissionId })
            .ToListAsync();

        Assert.Equal(permissionDefinitionsBefore, permissionDefinitionsAfter);
        Assert.Equal(roleDefinitionsBefore, roleDefinitionsAfter);
        Assert.Equal(roleGrantsBefore, roleGrantsAfter);
    }

    [Fact]
    public async Task CurrentSpaRoutesProvideUnambiguousAuthorizationForNav25ATargets()
    {
        var repoRoot = RepositoryRoot();
        var appRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "app-routing.module.ts"));
        var incomingRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "incoming-nacha-command-center",
            "incoming-nacha-command-center-routing.module.ts"));
        var transactionsRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "transactions",
            "transactions-routing.module.ts"));

        Assert.Contains("path: 'incoming-nacha-command-center'", appRouting);
        Assert.Contains("roles: ['Admin', 'ACH.Operator']", appRouting);
        Assert.Contains("permissions: ['CanReadAch']", appRouting);

        Assert.Contains("path: 'observability'", incomingRouting);
        Assert.Contains("path: 'queue'", incomingRouting);
        Assert.Contains("path: 'orphan-resolution'", incomingRouting);
        Assert.True(incomingRouting.Split("permissions: ['CanReadAch']", StringSplitOptions.None).Length >= 4);

        Assert.Contains("path: 'outgoing-monitoring'", transactionsRouting);
        Assert.Contains("'OutgoingTransactions.Monitor.Read'", transactionsRouting);
    }

    [Fact]
    public async Task CanonicalSeeder_LeavesOtherDomainsUntouchedAndIsIdempotentForNav25AScope()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        string[] untouchedRoutes =
        [
            "/nacha-config-admin/perfiles"
        ];
        var beforeUntouched = await ReadMenuSnapshotByRouteAsync(fixture.Context, untouchedRoutes);

        var seeder = new NavigationInformationArchitectureSeeder(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var afterUntouched = await ReadMenuSnapshotByRouteAsync(fixture.Context, untouchedRoutes);
        Assert.Equal(beforeUntouched, afterUntouched);

        var first = await ReadMonitoringSnapshotAsync(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var second = await ReadMonitoringSnapshotAsync(fixture.Context);
        Assert.Equal(first, second);
    }

    private static async Task<Guid[]> PermissionIdsAsync(AchDbContext context, int menuItemId)
        => await context.MenuItemPermissions
            .AsNoTracking()
            .Where(link => link.MenuItemId == menuItemId)
            .OrderBy(link => link.PermissionId)
            .Select(link => link.PermissionId)
            .ToArrayAsync();

    private static async Task<Guid[]> RoleIdsAsync(AchDbContext context, int menuItemId)
        => await context.MenuItemRoles
            .AsNoTracking()
            .Where(link => link.MenuItemId == menuItemId)
            .OrderBy(link => link.RoleId)
            .Select(link => link.RoleId)
            .ToArrayAsync();

    private static async Task<string[]> ReadMenuSnapshotByRouteAsync(AchDbContext context, IEnumerable<string> routes)
    {
        var routeSet = routes.ToHashSet(StringComparer.Ordinal);
        var rows = await context.MenuItems
            .AsNoTracking()
            .Where(item => routeSet.Contains(item.Route))
            .OrderBy(item => item.Route)
            .ThenBy(item => item.Id)
            .Select(item => new
            {
                item.Id,
                item.ParentId,
                item.Route,
                item.Label,
                item.Order,
                item.IsActive,
                item.Exact,
                item.Icon
            })
            .ToListAsync();

        return rows
            .Select(item => $"{item.Id}|{item.ParentId}|{item.Route}|{item.Label}|{item.Order}|{item.IsActive}|{item.Exact}|{item.Icon}")
            .ToArray();
    }

    private static async Task<string[]> ReadMonitoringSnapshotAsync(AchDbContext context)
    {
        var root = await context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.MonitoringAndControlRoute);

        var routeSet = MonitoringRoutes.Append(NavigationInformationArchitectureSeeder.MonitoringAndControlRoute)
            .ToHashSet(StringComparer.Ordinal);
        var itemRows = await context.MenuItems
            .AsNoTracking()
            .Where(item => routeSet.Contains(item.Route))
            .OrderBy(item => item.Id)
            .Select(item => new
            {
                item.Id,
                item.ParentId,
                item.Route,
                item.Label,
                item.Order,
                item.IsActive,
                item.Exact
            })
            .ToListAsync();

        Assert.Contains(itemRows, item => item.Id == root.Id);
        var itemIds = itemRows.Select(item => item.Id).ToArray();
        var items = itemRows.Select(item =>
            $"I|{item.Id}|{item.ParentId}|{item.Route}|{item.Label}|{item.Order}|{item.IsActive}|{item.Exact}");
        var permissionRows = await context.MenuItemPermissions
            .AsNoTracking()
            .Where(link => itemIds.Contains(link.MenuItemId))
            .OrderBy(link => link.MenuItemId)
            .ThenBy(link => link.PermissionId)
            .Select(link => new { link.MenuItemId, link.PermissionId })
            .ToListAsync();
        var permissions = permissionRows.Select(link => $"P|{link.MenuItemId}|{link.PermissionId}");
        var roleRows = await context.MenuItemRoles
            .AsNoTracking()
            .Where(link => itemIds.Contains(link.MenuItemId))
            .OrderBy(link => link.MenuItemId)
            .ThenBy(link => link.RoleId)
            .Select(link => new { link.MenuItemId, link.RoleId })
            .ToListAsync();
        var roles = roleRows.Select(link => $"R|{link.MenuItemId}|{link.RoleId}");

        return items.Concat(permissions).Concat(roles).ToArray();
    }

    private static async Task SeedFunctionalNavigationAsync(AchDbContext context)
    {
        IDbSeeder[] seeders =
        [
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

    private static string RepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "ACHInterbank.sln")))
            current = current.Parent;

        return current?.FullName
            ?? throw new DirectoryNotFoundException("No se encontró ACHInterbank.sln al resolver la raíz para NAV2.5A.");
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
