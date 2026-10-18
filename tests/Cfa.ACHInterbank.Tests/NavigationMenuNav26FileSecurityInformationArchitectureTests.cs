using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav26FileSecurityInformationArchitectureTests
{
    private static readonly string[] PublishedRoutes =
    [
        "/nacha-security/certificates",
        "/nacha-security/sobre-digital"
    ];

    private static readonly string[] TechnicalRoutes =
    [
        "/nacha-security/nacha/generate",
        "/nacha-security/nacha/generate-encrypted",
        "/nacha-security/digital-envelope/manual-encrypt",
        "/nacha-security/digital-envelope/manual-decrypt"
    ];

    [Fact]
    public async Task CanonicalSeeder_BuildsFileSecurityDomainWithDeterministicChildren()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var root = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.FileSecurityRoute);

        Assert.Null(root.ParentId);
        Assert.Equal(NavigationInformationArchitectureSeeder.FileSecurityLabel, root.Label);
        Assert.Equal(60, root.Order);

        var children = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == root.Id)
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order })
            .ToListAsync();

        (string Route, string Label, int Order)[] expected =
        [
            ("/nacha-security/certificates", "Certificados digitales", 10),
            ("/nacha-security/sobre-digital", "Cifrar y descifrar archivos", 20)
        ];

        Assert.Equal(expected, children.Select(item => (item.Route, item.Label, item.Order)).ToArray());
        Assert.Equal(children.Count, children.Select(item => item.Order).Distinct().Count());

        foreach (var route in PublishedRoutes)
            Assert.Single(await fixture.Context.MenuItems.AsNoTracking().Where(item => item.IsActive && item.Route == route).ToListAsync());

        foreach (var route in TechnicalRoutes)
            Assert.False(await fixture.Context.MenuItems.AsNoTracking().AnyAsync(item => item.IsActive && item.Route == route));
    }

    [Fact]
    public async Task CanonicalSeeder_PreservesExistingFileSecurityAuthorizationAndDoesNotChangeSecurityRoutes()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var beforeItems = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.Route == NavigationInformationArchitectureSeeder.FileSecurityRoute || PublishedRoutes.Contains(item.Route))
            .OrderBy(item => item.Route)
            .Select(item => new { item.Id, item.Route })
            .ToListAsync();
        Assert.Equal(3, beforeItems.Count);

        var ids = beforeItems.Select(item => item.Id).ToArray();
        var permissionsBefore = await ReadPermissionLinksAsync(fixture.Context, ids);
        var rolesBefore = await ReadRoleLinksAsync(fixture.Context, ids);
        var routesBefore = beforeItems.ToDictionary(item => item.Id, item => item.Route);

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var routesAfter = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Route);

        foreach (var (id, route) in routesBefore)
            Assert.Equal(route, routesAfter[id]);
        Assert.Equal(permissionsBefore, await ReadPermissionLinksAsync(fixture.Context, ids));
        Assert.Equal(rolesBefore, await ReadRoleLinksAsync(fixture.Context, ids));
    }

    [Fact]
    public async Task CurrentSpaKeepsPublishedAndTechnicalFileSecurityRoutesAvailable()
    {
        var routing = await File.ReadAllTextAsync(Path.Combine(
            RepositoryRoot(),
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "nacha-security",
            "nacha-security-routing.module.ts"));

        Assert.Contains("path: 'certificates'", routing);
        Assert.Contains("path: 'sobre-digital'", routing);
        Assert.Contains("path: 'nacha/generate'", routing);
        Assert.Contains("path: 'nacha/generate-encrypted'", routing);
        Assert.Contains("path: 'digital-envelope/manual-encrypt'", routing);
        Assert.Contains("path: 'digital-envelope/manual-decrypt'", routing);
    }

    [Fact]
    public async Task CanonicalSeeder_LeavesOtherPendingDomainsUntouchedAndIsIdempotentForNav26Scope()
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

        Assert.Equal(beforeUntouched, await ReadMenuSnapshotByRouteAsync(fixture.Context, untouchedRoutes));

        var first = await ReadFileSecuritySnapshotAsync(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var second = await ReadFileSecuritySnapshotAsync(fixture.Context);
        Assert.Equal(first, second);
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

    private static async Task<string[]> ReadMenuSnapshotByRouteAsync(AchDbContext context, string[] routes)
    {
        var rows = await context.MenuItems
            .AsNoTracking()
            .Where(item => routes.Contains(item.Route))
            .OrderBy(item => item.Route)
            .Select(item => new
            {
                item.Route,
                item.Label,
                item.ParentId,
                item.Order,
                item.IsActive,
                item.Exact,
                item.Icon
            })
            .ToListAsync();

        return rows
            .Select(item => $"{item.Route}|{item.Label}|{item.ParentId}|{item.Order}|{item.IsActive}|{item.Exact}|{item.Icon}")
            .ToArray();
    }

    private static async Task<string[]> ReadFileSecuritySnapshotAsync(AchDbContext context)
    {
        var items = await context.MenuItems
            .AsNoTracking()
            .Where(item => item.Route == NavigationInformationArchitectureSeeder.FileSecurityRoute || PublishedRoutes.Contains(item.Route))
            .OrderBy(item => item.Route)
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

        var ids = items.Select(item => item.Id).ToArray();
        var itemRows = items.Select(item =>
            $"I|{item.Id}|{item.Route}|{item.Label}|{item.ParentId}|{item.Order}|{item.IsActive}|{item.Exact}");
        var permissions = (await ReadPermissionLinksAsync(context, ids)).Select(value => $"P|{value}");
        var roles = (await ReadRoleLinksAsync(context, ids)).Select(value => $"R|{value}");
        return itemRows.Concat(permissions).Concat(roles).ToArray();
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

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ACHInterbank.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
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
