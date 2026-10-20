using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav28BAdministrationScheduledTasksInformationArchitectureTests
{
    private static readonly string[] ExistingSchedulerMenuRoutes =
    [
        "/scheduler",
        "/scheduler/tasks"
    ];

    [Fact]
    public async Task CanonicalSeeder_ReusesScheduledTasksAsDirectAdministrationChild()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var schedulerBefore = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.SchedulerId);
        var scheduledTasksBefore = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.TaskDefinitionsId);

        Assert.Equal("/scheduler", schedulerBefore.Route);
        Assert.Equal(NavigationInformationArchitectureSeeder.ScheduledTasksRoute, scheduledTasksBefore.Route);
        Assert.Equal(schedulerBefore.Id, scheduledTasksBefore.ParentId);

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
        Assert.Equal(NavigationInformationArchitectureSeeder.UsersAccessLabel, usersAccess.Label);
        Assert.Equal(10, usersAccess.Order);

        var scheduledTasks = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.TaskDefinitionsId);
        Assert.True(scheduledTasks.IsActive);
        Assert.Equal(scheduledTasksBefore.Id, scheduledTasks.Id);
        Assert.Equal(NavigationInformationArchitectureSeeder.ScheduledTasksRoute, scheduledTasks.Route);
        Assert.Equal(NavigationInformationArchitectureSeeder.ScheduledTasksLabel, scheduledTasks.Label);
        Assert.Equal(administration.Id, scheduledTasks.ParentId);
        Assert.Equal(20, scheduledTasks.Order);

        var legacySchedulerGroup = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.SchedulerId);
        Assert.Equal(schedulerBefore.Id, legacySchedulerGroup.Id);
        Assert.Equal("/scheduler", legacySchedulerGroup.Route);
        Assert.False(legacySchedulerGroup.IsActive);

        var administrationChildren = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == administration.Id)
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order })
            .ToListAsync();

        var administrationChildRows = administrationChildren
            .Select(item => (item.Route, item.Label, item.Order))
            .ToArray();
        Assert.Contains((Route: "/users", Label: "Usuarios y acceso", Order: 10), administrationChildRows);
        Assert.Contains((Route: "/scheduler/tasks", Label: "Tareas programadas", Order: 20), administrationChildRows);
    }

    [Fact]
    public async Task CanonicalSeeder_PreservesExistingSchedulerAuthorizationAndSecurityDefinitions()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var ids = new[] { MenuItemConfiguration.SchedulerId, MenuItemConfiguration.TaskDefinitionsId };
        var permissionLinksBefore = await ReadPermissionLinksAsync(fixture.Context, ids);
        var roleLinksBefore = await ReadRoleLinksAsync(fixture.Context, ids);
        var permissionDefinitionsBefore = await ReadPermissionDefinitionsAsync(fixture.Context);
        var roleDefinitionsBefore = await ReadRoleDefinitionsAsync(fixture.Context);
        var roleGrantsBefore = await ReadRoleGrantsAsync(fixture.Context);

        Assert.Equal(
            new[]
            {
                $"{MenuItemConfiguration.SchedulerId}|{PermissionConfiguration.SchedulerViewPermissionId}",
                $"{MenuItemConfiguration.TaskDefinitionsId}|{PermissionConfiguration.SchedulerViewPermissionId}"
            },
            permissionLinksBefore);
        Assert.Empty(roleLinksBefore);

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        Assert.Equal(permissionLinksBefore, await ReadPermissionLinksAsync(fixture.Context, ids));
        Assert.Equal(roleLinksBefore, await ReadRoleLinksAsync(fixture.Context, ids));
        Assert.Equal(permissionDefinitionsBefore, await ReadPermissionDefinitionsAsync(fixture.Context));
        Assert.Equal(roleDefinitionsBefore, await ReadRoleDefinitionsAsync(fixture.Context));
        Assert.Equal(roleGrantsBefore, await ReadRoleGrantsAsync(fixture.Context));
    }

    [Fact]
    public async Task CurrentSpaUsesSchedulerTasksAsTheSingleFunctionalSchedulerRouteWithSchedulerViewGuard()
    {
        var repoRoot = RepositoryRoot();
        var appRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "app-routing.module.ts"));
        var schedulerRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "scheduler",
            "scheduler-routing.module.ts"));

        Assert.Contains("path: 'scheduler'", appRouting);
        Assert.Contains("path: 'tasks'", schedulerRouting);
        Assert.Contains("permissions: ['Scheduler.View']", schedulerRouting);
        Assert.Contains("breadcrumb: 'Tareas programadas'", schedulerRouting);
        Assert.Contains("redirectTo: 'tasks'", schedulerRouting);

        Assert.DoesNotContain("path: 'history'", schedulerRouting);
        Assert.DoesNotContain("path: 'instances'", schedulerRouting);
        Assert.DoesNotContain("path: ':taskCode'", schedulerRouting);
        Assert.DoesNotContain("path: ':taskCode/history'", schedulerRouting);
        Assert.DoesNotContain("path: ':taskCode/technical'", schedulerRouting);
        Assert.DoesNotContain("path: ':taskCode/schedule'", schedulerRouting);
        Assert.DoesNotContain("path: ':taskCode/execute'", schedulerRouting);
    }

    [Fact]
    public async Task SchedulerDetailsHistoryExecutionAndScheduleRemainInPageOrApiOnlyAndOutOfMenu()
    {
        var repoRoot = RepositoryRoot();
        var component = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "scheduler",
            "components",
            "task-definitions.component.ts"));
        var service = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "scheduler",
            "services",
            "task-definitions.service.ts"));

        Assert.Contains("openDetail(task: SchedulerTask)", component);
        Assert.Contains("openHistory(task: SchedulerTask)", component);
        Assert.Contains("executeNow(", service);
        Assert.Contains("updateSchedule(", service);
        Assert.Contains("getTechnicalInfo(", service);
        Assert.Contains("getInstances()", service);

        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();
        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var activeSchedulerRoutes = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.Route.StartsWith("/scheduler"))
            .OrderBy(item => item.Route)
            .Select(item => item.Route)
            .ToArrayAsync();

        Assert.Equal(new[] { NavigationInformationArchitectureSeeder.ScheduledTasksRoute }, activeSchedulerRoutes);
    }

    [Fact]
    public async Task CanonicalSeeder_LeavesOtherOutOfScopeRoutesUntouchedAndIsIdempotentForNav28BScope()
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

        var first = await ReadNav28BSnapshotAsync(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var second = await ReadNav28BSnapshotAsync(fixture.Context);
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

    private static async Task<string[]> ReadNav28BSnapshotAsync(AchDbContext context)
    {
        var routes = ExistingSchedulerMenuRoutes
            .Append(NavigationInformationArchitectureSeeder.AdministrationRoute)
            .Append("/users")
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
