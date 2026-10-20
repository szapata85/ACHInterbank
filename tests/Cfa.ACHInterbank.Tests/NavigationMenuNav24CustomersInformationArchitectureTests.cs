using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav24CustomersInformationArchitectureTests
{
    [Fact]
    public async Task CanonicalSeeder_PublishesCustomersRootAndRetiresLegacyThirdPartiesFromVisibleMenu()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var legacyBefore = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == "/customer-third-parties");
        var legacyPermissionsBefore = await PermissionIdsAsync(fixture.Context, legacyBefore.Id);
        var legacyRolesBefore = await RoleIdsAsync(fixture.Context, legacyBefore.Id);

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var customers = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.CustomersRoute);

        Assert.True(customers.IsActive);
        Assert.Null(customers.ParentId);
        Assert.Equal("Clientes", customers.Label);
        Assert.Equal(30, customers.Order);
        Assert.False(customers.Exact);

        Assert.Equal(
            new[] { PermissionConfiguration.ReadAchPermissionId },
            await PermissionIdsAsync(fixture.Context, customers.Id));
        Assert.Empty(await RoleIdsAsync(fixture.Context, customers.Id));

        var legacyAfter = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Id == legacyBefore.Id);

        Assert.False(legacyAfter.IsActive);
        Assert.Equal(legacyBefore.Route, legacyAfter.Route);
        Assert.Equal(legacyBefore.Label, legacyAfter.Label);
        Assert.Equal(legacyBefore.ParentId, legacyAfter.ParentId);
        Assert.Equal(legacyBefore.Order, legacyAfter.Order);
        Assert.Equal(legacyBefore.Exact, legacyAfter.Exact);
        Assert.Equal(legacyBefore.Icon, legacyAfter.Icon);
        Assert.Equal(legacyPermissionsBefore, await PermissionIdsAsync(fixture.Context, legacyAfter.Id));
        Assert.Equal(legacyRolesBefore, await RoleIdsAsync(fixture.Context, legacyAfter.Id));

        Assert.False(await fixture.Context.MenuItems.AnyAsync(item =>
            item.IsActive && item.Route == "/customer-third-parties"));
    }

    [Fact]
    public async Task CanonicalSeeder_KeepsCustomerThirdPartyAdministrationContextualAndOutOfMenu()
    {
        var repoRoot = RepositoryRoot();
        var routingSource = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "customers",
            "customers-routing.module.ts"));

        Assert.Contains("path: ':id/third-parties'", routingSource);
        Assert.Contains("path: ':id/third-parties/:thirdPartyId'", routingSource);

        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var customers = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.CustomersRoute);

        Assert.False(await fixture.Context.MenuItems.AnyAsync(item =>
            item.IsActive && item.ParentId == customers.Id));
        string[] contextualMenuRoutes =
        [
            "/customers/:id/third-parties",
            "/customers/:id/third-parties/:thirdPartyId"
        ];
        Assert.False(await fixture.Context.MenuItems.AnyAsync(item => contextualMenuRoutes.Contains(item.Route)));
    }

    [Fact]
    public async Task CanonicalSeeder_PreservesOutOfScopeDomainsAndIsIdempotentForNav24Scope()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        string[] untouchedRoutes =
        [
            "/nacha-config-admin/perfiles"
        ];
        var beforeUntouched = await ReadMenuSnapshotByRouteAsync(fixture.Context, untouchedRoutes);

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

        var seeder = new NavigationInformationArchitectureSeeder(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var afterUntouched = await ReadMenuSnapshotByRouteAsync(fixture.Context, untouchedRoutes);
        Assert.Equal(beforeUntouched, afterUntouched);

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

        var first = await ReadCustomersSnapshotAsync(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var second = await ReadCustomersSnapshotAsync(fixture.Context);
        Assert.Equal(first, second);

        // Previously authorized domains remain canonical and untouched by NAV2.4 itself.
        Assert.True(await fixture.Context.MenuItems.AnyAsync(item =>
            item.IsActive
            && item.Route == NavigationInformationArchitectureSeeder.OperationRoute
            && item.Label == NavigationInformationArchitectureSeeder.OperationLabel));
        Assert.True(await fixture.Context.MenuItems.AnyAsync(item =>
            item.IsActive
            && item.Route == NavigationInformationArchitectureSeeder.ParametersAdministrationRoute
            && item.Label == NavigationInformationArchitectureSeeder.ParametersAdministrationLabel));
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

    private static async Task<string[]> ReadCustomersSnapshotAsync(AchDbContext context)
    {
        var itemRows = await context.MenuItems
            .AsNoTracking()
            .Where(item =>
                item.Route == NavigationInformationArchitectureSeeder.CustomersRoute
                || item.Route == "/customer-third-parties")
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

    private static string RepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "ACHInterbank.sln")))
            current = current.Parent;

        return current?.FullName
            ?? throw new DirectoryNotFoundException("No se encontró ACHInterbank.sln al resolver la raíz para NAV2.4.");
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
