using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav210AStructuralNormalizationTests
{
    [Fact]
    public async Task CanonicalSeeder_NormalizesNineRootDomainsAndLeavesNoActiveSiblingOrderCollisions()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var roots = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == null)
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order })
            .ToArrayAsync();

        Assert.Equal(
            new[]
            {
                (Route: "/dashboard", Label: "Inicio", Order: 10),
                (Route: "/transactions", Label: "Operación", Order: 20),
                (Route: "/customers", Label: "Clientes", Order: 30),
                (Route: "/seguimiento-control", Label: "Seguimiento y control", Order: 40),
                (Route: "/administracion-parametros", Label: "Administración de parámetros", Order: 50),
                (Route: "/nacha-security", Label: "Seguridad de archivos", Order: 60),
                (Route: "/reports", Label: "Reportes", Order: 70),
                (Route: "/administracion", Label: "Administración", Order: 80),
                (Route: "/uat", Label: "Herramientas de prueba", Order: 90)
            },
            roots.Select(item => (item.Route, item.Label, item.Order)).ToArray());

        var activeItems = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive)
            .Select(item => new { item.Id, item.ParentId, item.Route, item.Order })
            .ToListAsync();

        var duplicateSiblingOrders = activeItems
            .GroupBy(item => new { item.ParentId, item.Order })
            .Where(group => group.Count() > 1)
            .Select(group => new
            {
                group.Key.ParentId,
                group.Key.Order,
                Routes = group.Select(item => item.Route).OrderBy(route => route).ToArray()
            })
            .ToArray();

        Assert.Empty(duplicateSiblingOrders);
    }

    [Fact]
    public async Task CanonicalSeeder_ResolvesAchCyclesDuplicateWithoutChangingRouteOrAuthorization()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var cycleEntriesBefore = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.Route == "/ach-cycles")
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.Route })
            .ToArrayAsync();
        Assert.Equal(2, cycleEntriesBefore.Length);

        var cycleIds = cycleEntriesBefore.Select(item => item.Id).ToArray();
        var permissionsBefore = await ReadPermissionLinksAsync(fixture.Context, cycleIds);
        var rolesBefore = await ReadRoleLinksAsync(fixture.Context, cycleIds);
        var permissionDefinitionsBefore = await ReadPermissionDefinitionsAsync(fixture.Context);
        var roleDefinitionsBefore = await ReadRoleDefinitionsAsync(fixture.Context);
        var roleGrantsBefore = await ReadRoleGrantsAsync(fixture.Context);

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var parameters = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.ParametersAdministrationRoute);
        var canonicalCycles = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.AchCyclesId);

        Assert.True(canonicalCycles.IsActive);
        Assert.Equal("/ach-cycles", canonicalCycles.Route);
        Assert.Equal("Ciclos de operación", canonicalCycles.Label);
        Assert.Equal(parameters.Id, canonicalCycles.ParentId);
        Assert.Equal(30, canonicalCycles.Order);

        var duplicateCycles = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.Id != canonicalCycles.Id && item.Route == "/ach-cycles")
            .ToArrayAsync();
        Assert.NotEmpty(duplicateCycles);
        Assert.All(duplicateCycles, duplicate => Assert.False(duplicate.IsActive));
        Assert.Equal(1, await fixture.Context.MenuItems.CountAsync(item => item.IsActive && item.Route == "/ach-cycles"));

        Assert.Equal(permissionsBefore, await ReadPermissionLinksAsync(fixture.Context, cycleIds));
        Assert.Equal(rolesBefore, await ReadRoleLinksAsync(fixture.Context, cycleIds));
        Assert.Equal(permissionDefinitionsBefore, await ReadPermissionDefinitionsAsync(fixture.Context));
        Assert.Equal(roleDefinitionsBefore, await ReadRoleDefinitionsAsync(fixture.Context));
        Assert.Equal(roleGrantsBefore, await ReadRoleGrantsAsync(fixture.Context));
    }

    [Fact]
    public async Task CanonicalSeeder_PreservesApprovedLegacyVisibilityAndCanonicalDomainPlacements()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        foreach (var route in new[] { "/customer-third-parties", "/soap-integrations", "/scheduler" })
        {
            var legacy = await fixture.Context.MenuItems.AsNoTracking().SingleAsync(item => item.Route == route);
            Assert.False(legacy.IsActive);
        }

        Assert.False(await fixture.Context.MenuItems.AnyAsync(item => item.Route == "/transactions/bulk-create"));

        var serviceTests = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.ServiceTestsRoute);
        Assert.False(serviceTests.IsActive);

        var parameters = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.ParametersAdministrationRoute);
        var statusMappings = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == "/ach-responses/status-mappings");
        Assert.Equal(parameters.Id, statusMappings.ParentId);

        var systemRecords = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.SystemRecordsRoute);
        var navigationLog = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.NavigationLogRoute);
        Assert.Equal(systemRecords.Id, navigationLog.ParentId);

        var reports = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.ReportsRoute);
        var reportAudit = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == "/reports/audit");
        Assert.Equal(reports.Id, reportAudit.ParentId);
        Assert.NotEqual(systemRecords.Id, reportAudit.ParentId);

        var administration = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.AdministrationRoute);
        var platformCapabilities = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.PlatformCapabilitiesRoute);
        Assert.Equal(administration.Id, platformCapabilities.ParentId);

        var operation = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.OperationRoute);
        Assert.False(await fixture.Context.MenuItems.AnyAsync(item =>
            item.IsActive
            && item.ParentId == operation.Id
            && (item.Route == NavigationInformationArchitectureSeeder.CustomersRoute
                || item.Route == NavigationInformationArchitectureSeeder.MonitoringAndControlRoute)));
    }

    [Fact]
    public async Task CanonicalSeeder_IsIdempotentForNav210AStructuralState()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);

        var seeder = new NavigationInformationArchitectureSeeder(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var first = await ReadStructuralSnapshotAsync(fixture.Context);

        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var second = await ReadStructuralSnapshotAsync(fixture.Context);

        Assert.Equal(first, second);
    }

    private static async Task<string[]> ReadStructuralSnapshotAsync(AchDbContext context)
    {
        var items = await context.MenuItems
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(item => new
            {
                item.Id,
                item.Route,
                item.Label,
                item.ParentId,
                item.Order,
                item.IsActive,
                item.Exact
            })
            .ToListAsync();

        return items.Select(item =>
            $"{item.Id}|{item.Route}|{item.Label}|{item.ParentId}|{item.Order}|{item.IsActive}|{item.Exact}").ToArray();
    }

    private static async Task<string[]> ReadPermissionLinksAsync(AchDbContext context, int[] ids)
    {
        var rows = await context.MenuItemPermissions
            .AsNoTracking()
            .Where(link => ids.Contains(link.MenuItemId))
            .OrderBy(link => link.MenuItemId)
            .ThenBy(link => link.PermissionId)
            .Select(link => new { link.MenuItemId, link.PermissionId })
            .ToListAsync();
        return rows.Select(link => $"{link.MenuItemId}|{link.PermissionId}").ToArray();
    }

    private static async Task<string[]> ReadRoleLinksAsync(AchDbContext context, int[] ids)
    {
        var rows = await context.MenuItemRoles
            .AsNoTracking()
            .Where(link => ids.Contains(link.MenuItemId))
            .OrderBy(link => link.MenuItemId)
            .ThenBy(link => link.RoleId)
            .Select(link => new { link.MenuItemId, link.RoleId })
            .ToListAsync();
        return rows.Select(link => $"{link.MenuItemId}|{link.RoleId}").ToArray();
    }

    private static async Task<string[]> ReadPermissionDefinitionsAsync(AchDbContext context)
    {
        var rows = await context.Permissions
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.Name })
            .ToListAsync();
        return rows.Select(item => $"{item.Id}|{item.Name}").ToArray();
    }

    private static async Task<string[]> ReadRoleDefinitionsAsync(AchDbContext context)
    {
        var rows = await context.Roles
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.Name })
            .ToListAsync();
        return rows.Select(item => $"{item.Id}|{item.Name}").ToArray();
    }

    private static async Task<string[]> ReadRoleGrantsAsync(AchDbContext context)
    {
        var rows = await context.RolePermissions
            .AsNoTracking()
            .OrderBy(item => item.RoleId)
            .ThenBy(item => item.PermissionId)
            .Select(item => new { item.RoleId, item.PermissionId })
            .ToListAsync();
        return rows.Select(item => $"{item.RoleId}|{item.PermissionId}").ToArray();
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
