using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav27ReportsInformationArchitectureTests
{
    private static readonly (string Route, string Label, int Order)[] PublishedReports =
    [
        ("/reports/sent", "Transacciones enviadas", 10),
        ("/reports/received", "Transacciones recibidas", 20),
        ("/reports/returns", "Devoluciones", 30),
        ("/reports/rejections", "Rechazos", 40),
        ("/reports/files", "Archivos NACHA-M", 50),
        ("/reports/cycles", "Ciclos", 60),
        ("/reports/reconciliation", "Conciliación", 70),
        ("/reports/traceability", "Trazabilidad ACH", 80),
        ("/reports/history", "Histórico", 90),
        ("/reports/audit", "Auditoría", 100)
    ];

    [Fact]
    public async Task CanonicalSeeder_BuildsReportsDomainWithDeterministicChildren()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var publishedRoutes = PublishedReports.Select(report => report.Route).ToArray();
        Assert.False(await fixture.Context.MenuItems
            .AsNoTracking()
            .AnyAsync(item => publishedRoutes.Contains(item.Route)));

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var root = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.ReportsRoute);

        Assert.Null(root.ParentId);
        Assert.Equal(NavigationInformationArchitectureSeeder.ReportsLabel, root.Label);
        Assert.Equal(70, root.Order);

        var children = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == root.Id)
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order })
            .ToListAsync();

        Assert.Equal(PublishedReports, children.Select(item => (item.Route, item.Label, item.Order)).ToArray());
        Assert.Equal(children.Count, children.Select(item => item.Order).Distinct().Count());
        Assert.Equal(PublishedReports.Length, children.Count);

        foreach (var report in PublishedReports)
        {
            Assert.Single(await fixture.Context.MenuItems
                .AsNoTracking()
                .Where(item => item.IsActive && item.Route == report.Route)
                .ToListAsync());
        }
    }

    [Fact]
    public async Task CanonicalSeeder_ReusesReportRootAuthorizationOnlyForNewReportRows()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var rootBefore = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.ReportsRoute);
        var rootPermissionIds = await PermissionIdsAsync(fixture.Context, rootBefore.Id);
        var rootRoleIds = await RoleIdsAsync(fixture.Context, rootBefore.Id);
        Assert.NotEmpty(rootPermissionIds);
        Assert.NotEmpty(rootRoleIds);

        var existingIds = await fixture.Context.MenuItems
            .AsNoTracking()
            .Select(item => item.Id)
            .ToArrayAsync();
        var existingPermissionsBefore = await ReadPermissionLinksAsync(fixture.Context, existingIds);
        var existingRolesBefore = await ReadRoleLinksAsync(fixture.Context, existingIds);
        var permissionDefinitionsBefore = await ReadPermissionDefinitionsAsync(fixture.Context);
        var roleDefinitionsBefore = await ReadRoleDefinitionsAsync(fixture.Context);
        var roleGrantsBefore = await ReadRoleGrantsAsync(fixture.Context);

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        Assert.Equal(existingPermissionsBefore, await ReadPermissionLinksAsync(fixture.Context, existingIds));
        Assert.Equal(existingRolesBefore, await ReadRoleLinksAsync(fixture.Context, existingIds));
        Assert.Equal(permissionDefinitionsBefore, await ReadPermissionDefinitionsAsync(fixture.Context));
        Assert.Equal(roleDefinitionsBefore, await ReadRoleDefinitionsAsync(fixture.Context));
        Assert.Equal(roleGrantsBefore, await ReadRoleGrantsAsync(fixture.Context));

        foreach (var report in PublishedReports)
        {
            var item = await fixture.Context.MenuItems
                .AsNoTracking()
                .SingleAsync(candidate => candidate.Route == report.Route);
            Assert.Equal(rootPermissionIds, await PermissionIdsAsync(fixture.Context, item.Id));
            Assert.Equal(rootRoleIds, await RoleIdsAsync(fixture.Context, item.Id));
        }
    }

    [Fact]
    public async Task CurrentSpaDefinesAllPublishedReportRoutesUnderExistingReportsAuthorization()
    {
        var repoRoot = RepositoryRoot();
        var appRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "app-routing.module.ts"));
        var reportsRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "reports",
            "reports-routing.module.ts"));

        Assert.Contains("path: 'reports'", appRouting);
        Assert.Contains("roles: ['Admin', 'ACH.Operator']", appRouting);
        Assert.Contains("permissions: ['CanReadAch']", appRouting);

        string[] childPaths =
        [
            "sent",
            "received",
            "returns",
            "rejections",
            "files",
            "cycles",
            "reconciliation",
            "traceability",
            "history",
            "audit"
        ];
        foreach (var childPath in childPaths)
            Assert.Contains($"path: '{childPath}'", reportsRouting);
    }

    [Fact]
    public async Task ReportsAuditIsAReportSurfaceAndDoesNotReplaceSystemAuditLogs()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var reportsRoot = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.ReportsRoute);
        var reportAudit = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == "/reports/audit");
        var systemAudit = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == "/audit-logs");

        Assert.Equal(reportsRoot.Id, reportAudit.ParentId);
        Assert.NotEqual(reportsRoot.Id, systemAudit.ParentId);

        var repoRoot = RepositoryRoot();
        var reportsApi = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "core",
            "services",
            "reports-api.service.ts"));
        var auditLogApi = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "audit",
            "services",
            "audit-log.service.ts"));

        Assert.Contains("api/reports/audit", reportsApi);
        Assert.Contains("api/audit-logs", auditLogApi);
    }

    [Fact]
    public async Task CanonicalSeeder_LeavesPendingDomainsUntouchedAndIsIdempotentForNav27Scope()
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

        var first = await ReadReportsSnapshotAsync(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var second = await ReadReportsSnapshotAsync(fixture.Context);
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

    private static async Task<string[]> ReadReportsSnapshotAsync(AchDbContext context)
    {
        var routes = PublishedReports.Select(report => report.Route)
            .Prepend(NavigationInformationArchitectureSeeder.ReportsRoute)
            .ToArray();
        var items = await context.MenuItems
            .AsNoTracking()
            .Where(item => routes.Contains(item.Route))
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
