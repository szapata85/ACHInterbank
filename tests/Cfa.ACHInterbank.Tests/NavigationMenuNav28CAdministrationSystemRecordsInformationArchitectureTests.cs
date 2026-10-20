using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav28CAdministrationSystemRecordsInformationArchitectureTests
{
    private static readonly string[] ExistingSystemRecordsMenuRoutes =
    [
        "/logs",
        "/audit-logs",
        "/auth-logs"
    ];

    [Fact]
    public async Task CanonicalSeeder_CanonicalizesSystemRecordsUnderAdministrationWithApprovedOrders()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var recordsBefore = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.LogsId);
        var auditBefore = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.AuditLogId);
        var accessBefore = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.AuthLogId);

        Assert.Equal("/logs", recordsBefore.Route);
        Assert.Equal("/audit-logs", auditBefore.Route);
        Assert.Equal("/auth-logs", accessBefore.Route);
        Assert.False(await fixture.Context.MenuItems.AsNoTracking().AnyAsync(item => item.Route == "/navigation-logs"));

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var administration = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.AdministrationRoute);
        Assert.Null(administration.ParentId);
        Assert.Equal(NavigationInformationArchitectureSeeder.AdministrationLabel, administration.Label);
        Assert.Equal(80, administration.Order);

        var usersAccess = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == "/users");
        Assert.Equal(administration.Id, usersAccess.ParentId);
        Assert.Equal(10, usersAccess.Order);

        var scheduledTasks = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.ScheduledTasksRoute);
        Assert.Equal(administration.Id, scheduledTasks.ParentId);
        Assert.Equal(20, scheduledTasks.Order);

        var systemRecords = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.LogsId);
        Assert.True(systemRecords.IsActive);
        Assert.Equal(recordsBefore.Id, systemRecords.Id);
        Assert.Equal(NavigationInformationArchitectureSeeder.SystemRecordsRoute, systemRecords.Route);
        Assert.Equal(NavigationInformationArchitectureSeeder.SystemRecordsLabel, systemRecords.Label);
        Assert.Equal(administration.Id, systemRecords.ParentId);
        Assert.Equal(30, systemRecords.Order);

        var audit = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.AuditLogId);
        Assert.Equal(auditBefore.Id, audit.Id);
        Assert.Equal(NavigationInformationArchitectureSeeder.AuditLogRoute, audit.Route);
        Assert.Equal(NavigationInformationArchitectureSeeder.AuditLogLabel, audit.Label);
        Assert.Equal(systemRecords.Id, audit.ParentId);
        Assert.Equal(10, audit.Order);

        var access = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.AuthLogId);
        Assert.Equal(accessBefore.Id, access.Id);
        Assert.Equal(NavigationInformationArchitectureSeeder.AuthenticationLogRoute, access.Route);
        Assert.Equal(NavigationInformationArchitectureSeeder.AuthenticationLogLabel, access.Label);
        Assert.Equal(systemRecords.Id, access.ParentId);
        Assert.Equal(20, access.Order);

        var navigation = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.NavigationLogRoute);
        Assert.Equal(NavigationInformationArchitectureSeeder.NavigationLogLabel, navigation.Label);
        Assert.Equal(systemRecords.Id, navigation.ParentId);
        Assert.Equal(30, navigation.Order);

        var administrationChildren = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == administration.Id)
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order })
            .ToArrayAsync();

        var administrationChildRows = administrationChildren
            .Select(item => (item.Route, item.Label, item.Order))
            .ToArray();
        Assert.Contains((Route: "/users", Label: "Usuarios y acceso", Order: 10), administrationChildRows);
        Assert.Contains((Route: "/scheduler/tasks", Label: "Tareas programadas", Order: 20), administrationChildRows);
        Assert.Contains((Route: "/logs", Label: "Registros del sistema", Order: 30), administrationChildRows);

        var recordChildren = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == systemRecords.Id)
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order })
            .ToArrayAsync();

        Assert.Equal(
            new[]
            {
                (Route: "/audit-logs", Label: "Registro de auditoría", Order: 10),
                (Route: "/auth-logs", Label: "Registro de accesos", Order: 20),
                (Route: "/navigation-logs", Label: "Registro de navegación", Order: 30)
            },
            recordChildren.Select(item => (item.Route, item.Label, item.Order)).ToArray());
    }

    [Fact]
    public async Task CanonicalSeeder_PreservesExistingLogAuthorizationAndReusesCurrentNavigationReadPermissionOnlyForNewItem()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var existingIds = new[]
        {
            MenuItemConfiguration.LogsId,
            MenuItemConfiguration.AuditLogId,
            MenuItemConfiguration.AuthLogId
        };
        var permissionLinksBefore = await ReadPermissionLinksAsync(fixture.Context, existingIds);
        var roleLinksBefore = await ReadRoleLinksAsync(fixture.Context, existingIds);
        var permissionDefinitionsBefore = await ReadPermissionDefinitionsAsync(fixture.Context);
        var roleDefinitionsBefore = await ReadRoleDefinitionsAsync(fixture.Context);
        var roleGrantsBefore = await ReadRoleGrantsAsync(fixture.Context);

        Assert.Equal(
            new[]
            {
                $"{MenuItemConfiguration.AuditLogId}|{PermissionConfiguration.ManageUsersPermissionId}",
                $"{MenuItemConfiguration.AuthLogId}|{PermissionConfiguration.ManageUsersPermissionId}",
                $"{MenuItemConfiguration.LogsId}|{PermissionConfiguration.ManageUsersPermissionId}"
            },
            permissionLinksBefore);
        Assert.Empty(roleLinksBefore);

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        Assert.Equal(permissionLinksBefore, await ReadPermissionLinksAsync(fixture.Context, existingIds));
        Assert.Equal(roleLinksBefore, await ReadRoleLinksAsync(fixture.Context, existingIds));
        Assert.Equal(permissionDefinitionsBefore, await ReadPermissionDefinitionsAsync(fixture.Context));
        Assert.Equal(roleDefinitionsBefore, await ReadRoleDefinitionsAsync(fixture.Context));
        Assert.Equal(roleGrantsBefore, await ReadRoleGrantsAsync(fixture.Context));

        var navigation = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.NavigationLogRoute);
        Assert.Equal(
            new[] { $"{navigation.Id}|{PermissionConfiguration.ReadAchPermissionId}" },
            await ReadPermissionLinksAsync(fixture.Context, [navigation.Id]));
        Assert.Empty(await ReadRoleLinksAsync(fixture.Context, [navigation.Id]));
    }

    [Fact]
    public async Task CurrentSpaAndApiConfirmTheThreeSystemRecordSurfacesAndAuthorizationDriftIsNotChanged()
    {
        var repoRoot = RepositoryRoot();
        var appRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "app-routing.module.ts"));
        var auditRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "audit",
            "audit-routing.module.ts"));
        var authRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "auth-logs",
            "auth-logs-routing.module.ts"));
        var navigationRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "navigation-logs",
            "navigation-logs-routing.module.ts"));
        var auditController = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "src",
            "Cfa.ACHInterbank.Api",
            "Controllers",
            "AuditLogsController.cs"));
        var authController = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "src",
            "Cfa.ACHInterbank.Api",
            "Controllers",
            "AuthLogsController.cs"));
        var navigationController = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "src",
            "Cfa.ACHInterbank.Api",
            "Controllers",
            "NavigationLogsController.cs"));

        Assert.Contains("path: 'audit-logs'", appRouting);
        Assert.Contains("path: 'auth-logs'", appRouting);
        Assert.Contains("path: 'navigation-logs'", appRouting);
        Assert.Contains("path: 'log'", appRouting);
        Assert.Contains("path: 'logs'", appRouting);
        Assert.Contains("redirectTo: 'audit-logs'", appRouting);

        Assert.Contains("breadcrumb: 'Registro de auditoría'", auditRouting);
        Assert.Contains("breadcrumb: 'Registro de autenticaciones'", authRouting);
        Assert.Contains("breadcrumb: 'Log de navegación'", navigationRouting);
        Assert.DoesNotContain("permissionGuard", auditRouting);
        Assert.DoesNotContain("permissionGuard", authRouting);
        Assert.DoesNotContain("permissionGuard", navigationRouting);

        Assert.Contains("[Route(\"api/audit-logs\")]", auditController);
        Assert.Contains("[Authorize]", auditController);
        Assert.DoesNotContain("Authorize(Policy", auditController);

        Assert.Contains("[Route(\"api/auth-logs\")]", authController);
        Assert.Contains("[Authorize]", authController);
        Assert.DoesNotContain("Authorize(Policy", authController);

        Assert.Contains("[Route(\"api/navigation-logs\")]", navigationController);
        Assert.Contains("[HttpGet]", navigationController);
        Assert.Contains("[Authorize(Policy = \"CanReadAch\")]", navigationController);
    }

    [Fact]
    public async Task ReportsAuditRemainsExclusivelyUnderReportsAndDoesNotReplaceAdministrativeAudit()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();
        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var reports = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.ReportsRoute);
        var administration = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.AdministrationRoute);
        var systemRecords = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.SystemRecordsRoute);
        var reportAudit = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == "/reports/audit");
        var administrativeAudit = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.AuditLogRoute);

        Assert.Equal(reports.Id, reportAudit.ParentId);
        Assert.Equal(administration.Id, systemRecords.ParentId);
        Assert.Equal(systemRecords.Id, administrativeAudit.ParentId);
        Assert.NotEqual(reportAudit.ParentId, administrativeAudit.ParentId);
        Assert.Equal("Auditoría", reportAudit.Label);
        Assert.Equal("Registro de auditoría", administrativeAudit.Label);

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
    public async Task RedirectAliasesAndUnprovenTechnicalLogRoutesRemainOutsideTheMenu()
    {
        var repoRoot = RepositoryRoot();
        var auditRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "audit",
            "audit-routing.module.ts"));
        var authRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "auth-logs",
            "auth-logs-routing.module.ts"));
        var navigationRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "navigation-logs",
            "navigation-logs-routing.module.ts"));

        Assert.DoesNotContain("path: ':id'", auditRouting);
        Assert.DoesNotContain("path: ':id'", authRouting);
        Assert.DoesNotContain("path: ':id'", navigationRouting);

        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();
        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        Assert.False(await fixture.Context.MenuItems.AsNoTracking().AnyAsync(item => item.Route == "/log"));

        var systemRecords = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.SystemRecordsRoute);
        var childRoutes = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == systemRecords.Id)
            .OrderBy(item => item.Order)
            .Select(item => item.Route)
            .ToArrayAsync();

        Assert.Equal(
            new[]
            {
                NavigationInformationArchitectureSeeder.AuditLogRoute,
                NavigationInformationArchitectureSeeder.AuthenticationLogRoute,
                NavigationInformationArchitectureSeeder.NavigationLogRoute
            },
            childRoutes);
    }

    [Fact]
    public async Task CanonicalSeeder_LeavesOtherOutOfScopeRoutesUntouchedAndIsIdempotentForNav28CScope()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        string[] untouchedRoutes =
        [
            "/nacha-config-admin/perfiles"
        ];
        var untouchedBefore = await ReadMenuSnapshotByRouteAsync(fixture.Context, untouchedRoutes);

        var seeder = new NavigationInformationArchitectureSeeder(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        Assert.Equal(untouchedBefore, await ReadMenuSnapshotByRouteAsync(fixture.Context, untouchedRoutes));

        var first = await ReadNav28CSnapshotAsync(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var second = await ReadNav28CSnapshotAsync(fixture.Context);
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

    private static async Task<string[]> ReadNav28CSnapshotAsync(AchDbContext context)
    {
        var routes = ExistingSystemRecordsMenuRoutes
            .Append(NavigationInformationArchitectureSeeder.NavigationLogRoute)
            .Append(NavigationInformationArchitectureSeeder.AdministrationRoute)
            .Append("/users")
            .Append(NavigationInformationArchitectureSeeder.ScheduledTasksRoute)
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
