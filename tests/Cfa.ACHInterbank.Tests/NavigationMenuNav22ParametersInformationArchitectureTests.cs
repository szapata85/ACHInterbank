using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav22ParametersInformationArchitectureTests
{
    private static readonly HashSet<string> ExistingRoutesModifiedByCanonicalNavigation = new(StringComparer.Ordinal)
    {
        "/dashboard",
        "/clearing-houses",
        "/catalogs/clearing-house-preferences",
        "/ach-cycles",
        "/catalogs/bank-holidays",
        "/operational-restrictions",
        "/nacha-config-admin",
        "/catalogs",
        "/catalogs/financial-institutions",
        "/catalogs/clearing-house-special-dates",
        "/integraciones",
        "/soap-integrations",
        "/administracion/mft-ach",
        "/transactions",
        "/transactions/list",
        "/transactions/create",
        "/transactions/nacha-upload",
        "/transactions/returns",
        "/customer-third-parties",
        "/ach-claims",
        "/ach-colombia/file-exchange",
        "/ach-cycles/nacha/export",
        "/transactions/outgoing-monitoring",
        "/incoming-nacha-command-center",
        "/nacha-security",
        "/nacha-security/certificates",
        "/nacha-security/sobre-digital"
    };

    [Fact]
    public async Task CanonicalSeeder_BuildsParametersAdministrationWithDeterministicChildren()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var root = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item =>
                item.IsActive
                && item.Route == NavigationInformationArchitectureSeeder.ParametersAdministrationRoute);

        Assert.Null(root.ParentId);
        Assert.Equal("Administración de parámetros", root.Label);
        Assert.Equal(50, root.Order);

        var children = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == root.Id)
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order })
            .ToListAsync();

        (string Route, string Label, int Order)[] expected =
        [
            ("/clearing-houses", "Cámaras compensadoras", 10),
            ("/catalogs/clearing-house-preferences", "Prioridades por cámara", 20),
            ("/ach-cycles", "Ciclos de operación", 30),
            ("/catalogs/bank-holidays", "Días no hábiles", 40),
            ("/operational-restrictions", "Restricciones operativas", 50),
            ("/nacha-config-admin", "Configuración de archivos NACHA-M", 60),
            ("/catalogs", "Catálogos", 70),
            ("/integraciones", "Conexiones", 80),
            ("/ach-responses/status-mappings", "Homologación de estados", 90)
        ];

        Assert.Equal(expected, children.Select(item => (item.Route, item.Label, item.Order)).ToArray());
        Assert.Equal(children.Count, children.Select(item => item.Order).Distinct().Count());

        var connections = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == "/integraciones");
        var connectionChildren = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == connections.Id)
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order })
            .ToListAsync();

        (string Route, string Label, int Order)[] expectedConnectionChildren =
        [
            ("/integraciones/contrato", "Contrato del servicio", 10),
            ("/integraciones/relacion-informacion", "Relación de información", 20),
            ("/integraciones/conexion", "Conexión entidad participante", 30),
            ("/administracion/mft-ach", "Configuración del intercambio de archivos", 40)
        ];
        Assert.Equal(
            expectedConnectionChildren,
            connectionChildren.Select(item => (item.Route, item.Label, item.Order)).ToArray());

        var legacySoap = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == "/soap-integrations");
        Assert.False(legacySoap.IsActive);

        // NAV2.10A resolves the previously deferred duplicate while preserving the canonical route.
        Assert.Equal(
            1,
            await fixture.Context.MenuItems.CountAsync(item => item.IsActive && item.Route == "/ach-cycles"));
        Assert.False((await fixture.Context.MenuItems.SingleAsync(item =>
            item.Id != MenuItemConfiguration.AchCyclesId && item.Route == "/ach-cycles")).IsActive);
    }

    [Fact]
    public async Task CanonicalSeeder_PreservesExistingRoutesAuthorizationAndOutOfScopeMenuState()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var beforeItems = await fixture.Context.MenuItems
            .AsNoTracking()
            .ToDictionaryAsync(
                item => item.Id,
                item => new MenuSnapshot(
                    item.Route,
                    item.Label,
                    item.ParentId,
                    item.Order,
                    item.IsActive,
                    item.Exact,
                    item.Icon));
        var beforePermissionLinks = await fixture.Context.MenuItemPermissions
            .AsNoTracking()
            .ToListAsync();
        var beforePermissions = beforePermissionLinks
            .GroupBy(link => link.MenuItemId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(link => link.PermissionId).OrderBy(id => id).ToArray());
        var beforeRoleLinks = await fixture.Context.MenuItemRoles
            .AsNoTracking()
            .ToListAsync();
        var beforeRoles = beforeRoleLinks
            .GroupBy(link => link.MenuItemId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(link => link.RoleId).OrderBy(id => id).ToArray());

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var afterItems = await fixture.Context.MenuItems
            .AsNoTracking()
            .ToDictionaryAsync(
                item => item.Id,
                item => new MenuSnapshot(
                    item.Route,
                    item.Label,
                    item.ParentId,
                    item.Order,
                    item.IsActive,
                    item.Exact,
                    item.Icon));

        foreach (var (id, before) in beforeItems)
        {
            var after = afterItems[id];
            Assert.Equal(before.Route, after.Route);

            if (!ExistingRoutesModifiedByCanonicalNavigation.Contains(before.Route))
            {
                Assert.Equal(before, after);
            }
        }

        var existingIds = beforeItems.Keys.ToHashSet();
        var afterPermissionLinks = await fixture.Context.MenuItemPermissions
            .AsNoTracking()
            .ToListAsync();
        var afterPermissions = afterPermissionLinks
            .Where(link => existingIds.Contains(link.MenuItemId))
            .GroupBy(link => link.MenuItemId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(link => link.PermissionId).OrderBy(id => id).ToArray());
        var afterRoleLinks = await fixture.Context.MenuItemRoles
            .AsNoTracking()
            .ToListAsync();
        var afterRoles = afterRoleLinks
            .Where(link => existingIds.Contains(link.MenuItemId))
            .GroupBy(link => link.MenuItemId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(link => link.RoleId).OrderBy(id => id).ToArray());

        AssertAuthorizationEqual(beforeItems.Keys, beforePermissions, afterPermissions);
        AssertAuthorizationEqual(beforeItems.Keys, beforeRoles, afterRoles);
    }

    [Fact]
    public async Task CanonicalSeeder_IsIdempotentForNav22Scope()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        var seeder = new NavigationInformationArchitectureSeeder(fixture.Context);

        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var first = await ReadNav22SnapshotAsync(fixture.Context);

        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var second = await ReadNav22SnapshotAsync(fixture.Context);

        Assert.Equal(first, second);
    }

    private static async Task<string[]> ReadNav22SnapshotAsync(AchDbContext context)
    {
        var itemRows = await context.MenuItems
            .AsNoTracking()
            .Where(item =>
                item.Route == NavigationInformationArchitectureSeeder.ParametersAdministrationRoute
                || item.Route == "/clearing-houses"
                || item.Route == "/catalogs/clearing-house-preferences"
                || item.Route == "/ach-cycles"
                || item.Route == "/catalogs/bank-holidays"
                || item.Route == "/operational-restrictions"
                || item.Route == "/nacha-config-admin"
                || item.Route == "/catalogs"
                || item.Route == "/integraciones"
                || item.Route == "/soap-integrations"
                || item.Route == "/integraciones/contrato"
                || item.Route == "/integraciones/relacion-informacion"
                || item.Route == "/integraciones/conexion"
                || item.Route == "/administracion/mft-ach"
                || item.Route == "/ach-responses/status-mappings")
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
            .Select(item => $"{item.Id}|{item.ParentId}|{item.Route}|{item.Label}|{item.Order}|{item.IsActive}|{item.Exact}")
            .ToList();

        var itemIds = await context.MenuItems
            .AsNoTracking()
            .Where(item =>
                item.Route == "/integraciones/contrato"
                || item.Route == "/integraciones/relacion-informacion"
                || item.Route == "/integraciones/conexion"
                || item.Route == "/ach-responses/status-mappings")
            .Select(item => item.Id)
            .ToListAsync();

        var permissionRows = await context.MenuItemPermissions
            .AsNoTracking()
            .Where(link => itemIds.Contains(link.MenuItemId))
            .OrderBy(link => link.MenuItemId)
            .ThenBy(link => link.PermissionId)
            .Select(link => new { link.MenuItemId, link.PermissionId })
            .ToListAsync();
        var permissions = permissionRows
            .Select(link => $"P|{link.MenuItemId}|{link.PermissionId}")
            .ToList();
        var roleRows = await context.MenuItemRoles
            .AsNoTracking()
            .Where(link => itemIds.Contains(link.MenuItemId))
            .OrderBy(link => link.MenuItemId)
            .ThenBy(link => link.RoleId)
            .Select(link => new { link.MenuItemId, link.RoleId })
            .ToListAsync();
        var roles = roleRows
            .Select(link => $"R|{link.MenuItemId}|{link.RoleId}")
            .ToList();

        return items.Concat(permissions).Concat(roles).ToArray();
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

    private sealed record MenuSnapshot(
        string Route,
        string Label,
        int? ParentId,
        int Order,
        bool IsActive,
        bool Exact,
        string? Icon);

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
