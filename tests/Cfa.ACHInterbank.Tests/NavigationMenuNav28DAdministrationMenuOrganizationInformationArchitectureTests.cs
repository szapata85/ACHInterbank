using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav28DAdministrationMenuOrganizationInformationArchitectureTests
{
    [Fact]
    public async Task CanonicalSeeder_ReusesMenuOrganizationUnderAdministrationWithApprovedOrder()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var before = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.NavigationAdminId);

        Assert.Equal("/navigation/menu-items", before.Route);
        Assert.Equal("Navegación", before.Label);
        Assert.Null(before.ParentId);
        Assert.Equal(7, before.Order);

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var administration = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.AdministrationRoute);
        Assert.Null(administration.ParentId);
        Assert.Equal(NavigationInformationArchitectureSeeder.AdministrationLabel, administration.Label);
        Assert.Equal(80, administration.Order);

        var menuOrganization = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.NavigationAdminId);
        Assert.True(menuOrganization.IsActive);
        Assert.Equal(before.Id, menuOrganization.Id);
        Assert.Equal(NavigationInformationArchitectureSeeder.MenuOrganizationRoute, menuOrganization.Route);
        Assert.Equal(NavigationInformationArchitectureSeeder.MenuOrganizationLabel, menuOrganization.Label);
        Assert.Equal(administration.Id, menuOrganization.ParentId);
        Assert.Equal(40, menuOrganization.Order);

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
        Assert.Contains((Route: "/navigation/menu-items", Label: "Organización del menú", Order: 40), administrationChildRows);
    }

    [Fact]
    public async Task CanonicalSeeder_PreservesMenuOrganizationAuthorizationAndGlobalAuthorizationData()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var ids = new[] { MenuItemConfiguration.NavigationAdminId };
        var permissionLinksBefore = await ReadPermissionLinksAsync(fixture.Context, ids);
        var roleLinksBefore = await ReadRoleLinksAsync(fixture.Context, ids);
        var permissionDefinitionsBefore = await ReadPermissionDefinitionsAsync(fixture.Context);
        var roleDefinitionsBefore = await ReadRoleDefinitionsAsync(fixture.Context);
        var roleGrantsBefore = await ReadRoleGrantsAsync(fixture.Context);

        Assert.Equal(
            new[] { $"{MenuItemConfiguration.NavigationAdminId}|{PermissionConfiguration.ManageUsersPermissionId}" },
            permissionLinksBefore);
        Assert.Equal(
            new[] { $"{MenuItemConfiguration.NavigationAdminId}|{RoleConfiguration.AdminRoleId}" },
            roleLinksBefore);

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        Assert.Equal(permissionLinksBefore, await ReadPermissionLinksAsync(fixture.Context, ids));
        Assert.Equal(roleLinksBefore, await ReadRoleLinksAsync(fixture.Context, ids));
        Assert.Equal(permissionDefinitionsBefore, await ReadPermissionDefinitionsAsync(fixture.Context));
        Assert.Equal(roleDefinitionsBefore, await ReadRoleDefinitionsAsync(fixture.Context));
        Assert.Equal(roleGrantsBefore, await ReadRoleGrantsAsync(fixture.Context));
    }

    [Fact]
    public async Task CurrentSpaAndApiKeepMenuOrganizationRouteAfterAuthorizationNormalization()
    {
        var repoRoot = RepositoryRoot();
        var appRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "app-routing.module.ts"));
        var navigationRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "navigation",
            "navigation-routing.module.ts"));
        var menuItemsController = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "src",
            "Cfa.ACHInterbank.Api",
            "Controllers",
            "MenuItemsController.cs"));

        Assert.Contains("path: 'navigation'", appRouting);
        Assert.Contains("permissions: ['NavigationMenu.Manage', 'CanManageUsers']", appRouting);
        Assert.Contains("path: 'menu-items'", navigationRouting);
        Assert.Contains("redirectTo: 'menu-items'", navigationRouting);
        Assert.Contains("component: NavigationMenuComponent", navigationRouting);

        Assert.Contains("[Route(\"navigation/menu-items\")]", menuItemsController);
        Assert.Contains("[Route(\"api/navigation/menu-items\")]", menuItemsController);
        Assert.Contains("[Authorize(Policy = P1Policies.NavigationMenuManage)]", menuItemsController);
        Assert.DoesNotContain("Authorize(Roles", menuItemsController);
    }

    [Fact]
    public async Task NavigationRecordRemainsUnderSystemRecordsAndCapabilityRegistrySurfaceRemainsSeparate()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();
        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var administration = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.AdministrationRoute);
        var systemRecords = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.SystemRecordsRoute);
        var navigationRecord = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.NavigationLogRoute);
        var menuOrganization = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.MenuOrganizationRoute);

        Assert.Equal(administration.Id, systemRecords.ParentId);
        Assert.Equal(systemRecords.Id, navigationRecord.ParentId);
        Assert.Equal(30, navigationRecord.Order);
        Assert.Equal(administration.Id, menuOrganization.ParentId);
        Assert.Equal(40, menuOrganization.Order);
        Assert.NotEqual(navigationRecord.ParentId, menuOrganization.ParentId);

        var repoRoot = RepositoryRoot();
        var appRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "app-routing.module.ts"));
        var capabilityController = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "src",
            "Cfa.ACHInterbank.Api",
            "Controllers",
            "PaymentRailCapabilityRegistryController.cs"));
        var authService = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "core",
            "services",
            "auth.service.ts"));

        Assert.Contains("path: 'payment-rail-capability-registry'", appRouting);
        Assert.Contains("permissions: ['CanViewPaymentRailCapabilityRegistry', 'CanManageAch', 'CanReadAch']", appRouting);
        Assert.Contains("permissions.some((permission) => session.permissions.includes(permission))", authService);
        Assert.Contains("[Authorize(Policy = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)]", capabilityController);
    }

    [Fact]
    public async Task CanonicalSeeder_LeavesOtherOutOfScopeRoutesUntouchedAndIsIdempotentForNav28DScope()
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

        var first = await ReadNav28DSnapshotAsync(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var second = await ReadNav28DSnapshotAsync(fixture.Context);
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

    private static async Task<string[]> ReadNav28DSnapshotAsync(AchDbContext context)
    {
        var routes = new[]
        {
            NavigationInformationArchitectureSeeder.AdministrationRoute,
            "/users",
            NavigationInformationArchitectureSeeder.ScheduledTasksRoute,
            NavigationInformationArchitectureSeeder.SystemRecordsRoute,
            NavigationInformationArchitectureSeeder.AuditLogRoute,
            NavigationInformationArchitectureSeeder.AuthenticationLogRoute,
            NavigationInformationArchitectureSeeder.NavigationLogRoute,
            NavigationInformationArchitectureSeeder.MenuOrganizationRoute
        };

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
