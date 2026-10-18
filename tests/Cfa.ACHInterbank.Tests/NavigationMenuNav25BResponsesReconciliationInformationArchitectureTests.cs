using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests
{
    private static readonly string[] ResponseRoutes =
    [
        "/ach-responses",
        "/ach-responses/dashboard",
        "/ach-responses/manual-review",
        "/ach-responses/audit"
    ];

    [Fact]
    public async Task CanonicalSeeder_CompletesMonitoringAndControlWithResponsesAndReconciliation()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        Assert.False(await fixture.Context.MenuItems.AnyAsync(item => item.Route == "/ach-responses"));
        Assert.False(await fixture.Context.MenuItems.AnyAsync(item => item.Route == "/ach/reconciliation"));

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var root = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.MonitoringAndControlRoute);

        var rootChildren = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == root.Id)
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order })
            .ToListAsync();

        (string Route, string Label, int Order)[] expectedRootChildren =
        [
            ("/transactions/outgoing-monitoring", "Transacciones enviadas", 10),
            ("/incoming-nacha-command-center", "Seguimiento de archivos", 20),
            ("/incoming-nacha-command-center/observability", "Indicadores operativos", 30),
            ("/incoming-nacha-command-center/queue", "Cola de procesamiento", 40),
            ("/incoming-nacha-command-center/orphan-resolution", "Devoluciones sin relación", 50),
            (NavigationInformationArchitectureSeeder.TransactionResponsesGroupRoute, "Respuestas de transacciones", 60),
            ("/ach/reconciliation", "Conciliación", 70)
        ];

        Assert.Equal(
            expectedRootChildren,
            rootChildren.Select(item => (item.Route, item.Label, item.Order)).ToArray());
        Assert.Equal(rootChildren.Count, rootChildren.Select(item => item.Order).Distinct().Count());

        var responsesGroup = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item =>
                item.IsActive
                && item.Route == NavigationInformationArchitectureSeeder.TransactionResponsesGroupRoute);
        Assert.Equal(root.Id, responsesGroup.ParentId);
        Assert.Equal(NavigationInformationArchitectureSeeder.TransactionResponsesGroupLabel, responsesGroup.Label);
        Assert.Equal(60, responsesGroup.Order);
        Assert.False(responsesGroup.Exact);
        Assert.NotEqual("/ach-responses", responsesGroup.Route);

        var responseChildren = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == responsesGroup.Id)
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order })
            .ToListAsync();

        (string Route, string Label, int Order)[] expectedResponseChildren =
        [
            ("/ach-responses", "Bandeja de respuestas", 10),
            ("/ach-responses/dashboard", "Resumen de respuestas", 20),
            ("/ach-responses/manual-review", "Revisión manual", 30),
            ("/ach-responses/audit", "Historial de respuestas", 40)
        ];

        Assert.Equal(
            expectedResponseChildren,
            responseChildren.Select(item => (item.Route, item.Label, item.Order)).ToArray());
        Assert.Equal(responseChildren.Count, responseChildren.Select(item => item.Order).Distinct().Count());

        var reconciliation = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == "/ach/reconciliation");
        Assert.Equal(root.Id, reconciliation.ParentId);
        Assert.Equal(70, reconciliation.Order);

        var statusMappings = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == "/ach-responses/status-mappings");
        var parametersRoot = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.ParametersAdministrationRoute);
        Assert.Equal(parametersRoot.Id, statusMappings.ParentId);
        Assert.NotEqual(root.Id, statusMappings.ParentId);
        Assert.NotEqual(responsesGroup.Id, statusMappings.ParentId);

        Assert.False(await fixture.Context.MenuItems.AnyAsync(item => item.Route.Contains(":id")));
        Assert.False(await fixture.Context.MenuItems.AnyAsync(item => item.Route.Contains("notification-attempts")));
    }

    [Fact]
    public async Task CanonicalSeeder_ReusesCurrentReadAuthorizationOnlyForNewNav25BFunctionalRows()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var existingMenuIds = await fixture.Context.MenuItems
            .AsNoTracking()
            .Select(item => item.Id)
            .ToArrayAsync();
        var existingPermissionLinksBefore = await ReadPermissionLinksAsync(fixture.Context, existingMenuIds);
        var existingRoleLinksBefore = await ReadRoleLinksAsync(fixture.Context, existingMenuIds);

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

        Assert.Equal(existingPermissionLinksBefore, await ReadPermissionLinksAsync(fixture.Context, existingMenuIds));
        Assert.Equal(existingRoleLinksBefore, await ReadRoleLinksAsync(fixture.Context, existingMenuIds));

        foreach (var route in ResponseRoutes.Append("/ach/reconciliation"))
        {
            var item = await fixture.Context.MenuItems
                .AsNoTracking()
                .SingleAsync(candidate => candidate.Route == route);

            Assert.Equal(
                new[] { PermissionConfiguration.ReadAchPermissionId },
                await PermissionIdsAsync(fixture.Context, item.Id));
            Assert.Equal(
                new[] { RoleConfiguration.AdminRoleId, RoleConfiguration.OperatorRoleId }.OrderBy(id => id).ToArray(),
                await RoleIdsAsync(fixture.Context, item.Id));
        }

        var responsesGroup = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.TransactionResponsesGroupRoute);
        Assert.Empty(await PermissionIdsAsync(fixture.Context, responsesGroup.Id));
        Assert.Empty(await RoleIdsAsync(fixture.Context, responsesGroup.Id));

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
    public async Task ManualReviewMenu_RemainsReadAuthorized_WhileStatusMappingsStayUnderParameters()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var manualReview = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == "/ach-responses/manual-review");

        Assert.Equal(
            new[] { PermissionConfiguration.ReadAchPermissionId },
            await PermissionIdsAsync(fixture.Context, manualReview.Id));
        Assert.Equal(
            new[] { RoleConfiguration.AdminRoleId, RoleConfiguration.OperatorRoleId }.OrderBy(id => id).ToArray(),
            await RoleIdsAsync(fixture.Context, manualReview.Id));

        var statusMappings = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == "/ach-responses/status-mappings");
        var parametersRoot = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.ParametersAdministrationRoute);

        Assert.Equal(parametersRoot.Id, statusMappings.ParentId);
    }

    [Fact]
    public async Task CurrentSpaRoutesProvideUnambiguousAuthorizationForNav25BTargetsAndKeepDetailsContextual()
    {
        var repoRoot = RepositoryRoot();
        var appRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "app-routing.module.ts"));
        var responsesRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "ach-responses",
            "ach-responses-routing.module.ts"));
        var operationalRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "nacha-operational",
            "nacha-operational-routing.module.ts"));

        Assert.Contains("path: 'ach-responses'", appRouting);
        Assert.Contains("path: 'ach'", appRouting);
        Assert.True(appRouting.Split("roles: ['Admin', 'ACH.Operator']", StringSplitOptions.None).Length >= 3);
        Assert.True(appRouting.Split("permissions: ['CanReadAch']", StringSplitOptions.None).Length >= 3);

        Assert.Contains("path: ''", responsesRouting);
        Assert.Contains("path: 'dashboard'", responsesRouting);
        Assert.Contains("path: 'manual-review'", responsesRouting);
        Assert.Contains("path: 'audit'", responsesRouting);
        Assert.True(responsesRouting.Split("permissions: ['CanReadAch']", StringSplitOptions.None).Length >= 7);
        Assert.Contains("path: ':id'", responsesRouting);
        Assert.Contains("path: ':id/notification-attempts'", responsesRouting);

        Assert.Contains("path: 'reconciliation'", operationalRouting);
        Assert.Contains("permissions: ['CanReadAch']", operationalRouting);
    }

    [Fact]
    public async Task CanonicalSeeder_LeavesOutOfScopeDomainsUntouchedAndIsIdempotentForNav25BScope()
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

        var first = await ReadNav25BSnapshotAsync(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var second = await ReadNav25BSnapshotAsync(fixture.Context);
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

    private static async Task<string[]> ReadPermissionLinksAsync(AchDbContext context, int[] menuItemIds)
    {
        var rows = await context.MenuItemPermissions
            .AsNoTracking()
            .Where(link => menuItemIds.Contains(link.MenuItemId))
            .OrderBy(link => link.MenuItemId)
            .ThenBy(link => link.PermissionId)
            .Select(link => new { link.MenuItemId, link.PermissionId })
            .ToListAsync();
        return rows.Select(link => $"{link.MenuItemId}|{link.PermissionId}").ToArray();
    }

    private static async Task<string[]> ReadRoleLinksAsync(AchDbContext context, int[] menuItemIds)
    {
        var rows = await context.MenuItemRoles
            .AsNoTracking()
            .Where(link => menuItemIds.Contains(link.MenuItemId))
            .OrderBy(link => link.MenuItemId)
            .ThenBy(link => link.RoleId)
            .Select(link => new { link.MenuItemId, link.RoleId })
            .ToListAsync();
        return rows.Select(link => $"{link.MenuItemId}|{link.RoleId}").ToArray();
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

    private static async Task<string[]> ReadNav25BSnapshotAsync(AchDbContext context)
    {
        var routeSet = ResponseRoutes
            .Append("/ach/reconciliation")
            .Append(NavigationInformationArchitectureSeeder.TransactionResponsesGroupRoute)
            .Append(NavigationInformationArchitectureSeeder.MonitoringAndControlRoute)
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
            ?? throw new DirectoryNotFoundException("No se encontró ACHInterbank.sln al resolver la raíz para NAV2.5B.");
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
