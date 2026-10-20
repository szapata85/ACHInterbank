using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav23OperationInformationArchitectureTests
{
    private static readonly string[] CanonicalOperationRoutes =
    [
        "/transactions/list",
        "/transactions/create",
        "/transactions/bulk-ingestion/upload",
        "/transactions/bulk-ingestion/tracking",
        "/transactions/nacha-upload",
        "/transactions/returns",
        "/transactions/returns-ror",
        "/ach-claims",
        "/ach-colombia/file-exchange",
        "/ach-cycles/nacha/export"
    ];

    [Fact]
    public async Task CanonicalSeeder_BuildsOperationWithExpectedRoutesLabelsAndUniqueSiblingOrders()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var operation = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item =>
                item.IsActive
                && item.Route == NavigationInformationArchitectureSeeder.OperationRoute);

        Assert.Null(operation.ParentId);
        Assert.Equal("Operación", operation.Label);
        Assert.Equal(20, operation.Order);

        var canonicalChildren = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item =>
                item.IsActive
                && item.ParentId == operation.Id
                && CanonicalOperationRoutes.Contains(item.Route))
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order })
            .ToListAsync();

        (string Route, string Label, int Order)[] expected =
        [
            ("/transactions/list", "Consultar transacciones", 10),
            ("/transactions/create", "Crear transacción", 20),
            ("/transactions/bulk-ingestion/upload", "Cargar transacciones desde archivo", 30),
            ("/transactions/bulk-ingestion/tracking", "Seguimiento de cargas", 40),
            ("/transactions/nacha-upload", "Cargar archivo NACHA-M", 50),
            ("/transactions/returns", "Devoluciones", 60),
            ("/transactions/returns-ror", "Devolución de una devolución", 70),
            ("/ach-claims", "Reclamaciones", 80),
            ("/ach-colombia/file-exchange", "Intercambio de archivos", 90),
            ("/ach-cycles/nacha/export", "Exportar archivo NACHA-M", 100)
        ];

        Assert.Equal(expected, canonicalChildren.Select(item => (item.Route, item.Label, item.Order)).ToArray());

        var allActiveChildOrders = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == operation.Id)
            .Select(item => item.Order)
            .ToListAsync();
        Assert.Equal(allActiveChildOrders.Count, allActiveChildOrders.Distinct().Count());

        Assert.False(await fixture.Context.MenuItems.AnyAsync(item => item.Route == "/transactions/bulk-create"));

        // NAV2.10A resolves the previously deferred duplicate without changing the route.
        Assert.Equal(
            1,
            await fixture.Context.MenuItems.CountAsync(item => item.IsActive && item.Route == "/ach-cycles"));
    }

    [Fact]
    public async Task CanonicalSeeder_PreservesExistingAuthorizationAndUsesCurrentRouteAuthorizationOnlyForNewOperationItems()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var beforeItemIds = await fixture.Context.MenuItems
            .AsNoTracking()
            .Select(item => item.Id)
            .ToListAsync();
        var beforePermissions = await ReadPermissionLinksAsync(fixture.Context, beforeItemIds);
        var beforeRoles = await ReadRoleLinksAsync(fixture.Context, beforeItemIds);
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

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var afterPermissions = await ReadPermissionLinksAsync(fixture.Context, beforeItemIds);
        var afterRoles = await ReadRoleLinksAsync(fixture.Context, beforeItemIds);
        AssertAuthorizationEqual(beforeItemIds, beforePermissions, afterPermissions);
        AssertAuthorizationEqual(beforeItemIds, beforeRoles, afterRoles);

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
        Assert.Equal(permissionDefinitionsBefore, permissionDefinitionsAfter);
        Assert.Equal(roleDefinitionsBefore, roleDefinitionsAfter);

        foreach (var route in new[]
                 {
                     "/transactions/bulk-ingestion/upload",
                     "/transactions/bulk-ingestion/tracking",
                     "/transactions/returns-ror"
                 })
        {
            var itemId = await fixture.Context.MenuItems
                .AsNoTracking()
                .Where(item => item.Route == route)
                .Select(item => item.Id)
                .SingleAsync();

            var permissions = await fixture.Context.MenuItemPermissions
                .AsNoTracking()
                .Where(link => link.MenuItemId == itemId)
                .Select(link => link.PermissionId)
                .OrderBy(id => id)
                .ToArrayAsync();
            Assert.Equal(new[] { PermissionConfiguration.ManageAchPermissionId }, permissions);

            var roles = await fixture.Context.MenuItemRoles
                .AsNoTracking()
                .Where(link => link.MenuItemId == itemId)
                .Select(link => link.RoleId)
                .OrderBy(id => id)
                .ToArrayAsync();
            Assert.Equal(
                new[] { RoleConfiguration.AdminRoleId, RoleConfiguration.OperatorRoleId }.OrderBy(id => id).ToArray(),
                roles);
        }
    }

    [Fact]
    public async Task CanonicalSeeder_LeavesOtherDomainsUntouchedAndIsIdempotentForNav23Scope()
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

        var first = await ReadOperationSnapshotAsync(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var second = await ReadOperationSnapshotAsync(fixture.Context);
        Assert.Equal(first, second);

        var parametersRoot = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.ParametersAdministrationRoute);
        var parameterChildren = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == parametersRoot.Id)
            .OrderBy(item => item.Order)
            .Select(item => item.Route)
            .ToArrayAsync();
        Assert.Equal(
            new[]
            {
                "/clearing-houses",
                "/catalogs/clearing-house-preferences",
                "/ach-cycles",
                "/catalogs/bank-holidays",
                "/operational-restrictions",
                "/nacha-config-admin",
                "/catalogs",
                "/integraciones",
                "/ach-responses/status-mappings"
            },
            parameterChildren);
    }

    private static async Task<Dictionary<int, Guid[]>> ReadPermissionLinksAsync(
        AchDbContext context,
        IEnumerable<int> itemIds)
    {
        var ids = itemIds.ToHashSet();
        var links = await context.MenuItemPermissions
            .AsNoTracking()
            .Where(link => ids.Contains(link.MenuItemId))
            .ToListAsync();
        return links
            .GroupBy(link => link.MenuItemId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(link => link.PermissionId).OrderBy(id => id).ToArray());
    }

    private static async Task<Dictionary<int, Guid[]>> ReadRoleLinksAsync(
        AchDbContext context,
        IEnumerable<int> itemIds)
    {
        var ids = itemIds.ToHashSet();
        var links = await context.MenuItemRoles
            .AsNoTracking()
            .Where(link => ids.Contains(link.MenuItemId))
            .ToListAsync();
        return links
            .GroupBy(link => link.MenuItemId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(link => link.RoleId).OrderBy(id => id).ToArray());
    }

    private static void AssertAuthorizationEqual(
        IEnumerable<int> itemIds,
        IReadOnlyDictionary<int, Guid[]> before,
        IReadOnlyDictionary<int, Guid[]> after)
    {
        foreach (var id in itemIds)
        {
            before.TryGetValue(id, out var beforeIds);
            after.TryGetValue(id, out var afterIds);
            Assert.Equal(beforeIds ?? [], afterIds ?? []);
        }
    }

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

    private static async Task<string[]> ReadOperationSnapshotAsync(AchDbContext context)
    {
        var operation = await context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.OperationRoute);

        var itemRows = await context.MenuItems
            .AsNoTracking()
            .Where(item => item.Id == operation.Id || item.ParentId == operation.Id)
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
        var items = itemRows
            .Select(item => $"I|{item.Id}|{item.ParentId}|{item.Route}|{item.Label}|{item.Order}|{item.IsActive}|{item.Exact}")
            .ToList();

        var itemIds = itemRows.Select(item => item.Id).ToArray();
        var permissionRows = await context.MenuItemPermissions
            .AsNoTracking()
            .Where(link => itemIds.Contains(link.MenuItemId))
            .OrderBy(link => link.MenuItemId)
            .ThenBy(link => link.PermissionId)
            .Select(link => new { link.MenuItemId, link.PermissionId })
            .ToListAsync();
        var permissions = permissionRows
            .Select(link => $"P|{link.MenuItemId}|{link.PermissionId}");
        var roleRows = await context.MenuItemRoles
            .AsNoTracking()
            .Where(link => itemIds.Contains(link.MenuItemId))
            .OrderBy(link => link.MenuItemId)
            .ThenBy(link => link.RoleId)
            .Select(link => new { link.MenuItemId, link.RoleId })
            .ToListAsync();
        var roles = roleRows
            .Select(link => $"R|{link.MenuItemId}|{link.RoleId}");

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
