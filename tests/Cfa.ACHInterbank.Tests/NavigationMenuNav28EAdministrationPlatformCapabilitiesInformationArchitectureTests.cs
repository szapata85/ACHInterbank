using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav28EAdministrationPlatformCapabilitiesInformationArchitectureTests
{
    [Fact]
    public async Task CanonicalSeeder_PublishesPlatformCapabilitiesUnderAdministrationWithApprovedOrderAndRoute()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        Assert.False(await fixture.Context.MenuItems
            .AsNoTracking()
            .AnyAsync(item => item.Route == NavigationInformationArchitectureSeeder.PlatformCapabilitiesRoute));

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var administration = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.AdministrationRoute);
        Assert.Null(administration.ParentId);
        Assert.Equal(NavigationInformationArchitectureSeeder.AdministrationLabel, administration.Label);
        Assert.Equal(80, administration.Order);

        var platformCapabilities = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.PlatformCapabilitiesRoute);
        Assert.Equal(NavigationInformationArchitectureSeeder.PlatformCapabilitiesLabel, platformCapabilities.Label);
        Assert.Equal(administration.Id, platformCapabilities.ParentId);
        Assert.Equal(50, platformCapabilities.Order);
        Assert.True(platformCapabilities.Exact);

        var administrationChildren = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == administration.Id)
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order })
            .ToArrayAsync();

        Assert.Equal(
            new[]
            {
                (Route: "/users", Label: "Usuarios y acceso", Order: 10),
                (Route: "/scheduler/tasks", Label: "Tareas programadas", Order: 20),
                (Route: "/logs", Label: "Registros del sistema", Order: 30),
                (Route: "/navigation/menu-items", Label: "Organización del menú", Order: 40),
                (Route: "/payment-rail-capability-registry", Label: "Capacidades de la plataforma", Order: 50)
            },
            administrationChildren.Select(item => (item.Route, item.Label, item.Order)).ToArray());
    }

    [Fact]
    public async Task NewPlatformCapabilitiesMenuItem_ReusesExistingReadAuthorizationWithoutChangingPermissionsRolesOrGrants()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var permissionDefinitionsBefore = await ReadPermissionDefinitionsAsync(fixture.Context);
        var roleDefinitionsBefore = await ReadRoleDefinitionsAsync(fixture.Context);
        var roleGrantsBefore = await ReadRoleGrantsAsync(fixture.Context);

        Assert.False(permissionDefinitionsBefore.Any(
            row => row.EndsWith($"|{FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry}", StringComparison.Ordinal)));

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var platformCapabilities = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.PlatformCapabilitiesRoute);

        Assert.Equal(
            new[] { $"{platformCapabilities.Id}|{PermissionConfiguration.ReadAchPermissionId}" },
            await ReadPermissionLinksAsync(fixture.Context, [platformCapabilities.Id]));
        Assert.Equal(
            new[]
            {
                $"{platformCapabilities.Id}|{RoleConfiguration.AdminRoleId}",
                $"{platformCapabilities.Id}|{RoleConfiguration.OperatorRoleId}"
            },
            await ReadRoleLinksAsync(fixture.Context, [platformCapabilities.Id]));

        Assert.Equal(permissionDefinitionsBefore, await ReadPermissionDefinitionsAsync(fixture.Context));
        Assert.Equal(roleDefinitionsBefore, await ReadRoleDefinitionsAsync(fixture.Context));
        Assert.Equal(roleGrantsBefore, await ReadRoleGrantsAsync(fixture.Context));
    }

    [Fact]
    public async Task CurrentImplementation_ConfirmsReadOnlyAdministrativeGovernancePurposeAndEffectiveAuthorizationFallback()
    {
        var repoRoot = RepositoryRoot();
        var appRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "app-routing.module.ts"));
        var pageHtml = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "payment-rail-capability-registry",
            "pages",
            "payment-rail-capability-registry-page.component.html"));
        var apiService = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "payment-rail-capability-registry",
            "services",
            "payment-rail-capability-registry-api.service.ts"));
        var capabilityController = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "src",
            "Cfa.ACHInterbank.Api",
            "Controllers",
            "PaymentRailCapabilityRegistryController.cs"));
        var externalDi = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "src",
            "Cfa.ACHInterbank.External",
            "DependencyInjectionService.cs"));
        var permissionConfiguration = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "src",
            "Cfa.ACHInterbank.Persistence",
            "Configuration",
            "PermissionConfiguration.cs"));

        var routeBlock = Slice(appRouting, "path: 'payment-rail-capability-registry'", "path: 'unauthorized'");
        // NAV3.4I later removed the SPA role-name gate while preserving the same permission alternatives.
        Assert.Contains("canActivate: [permissionGuard]", routeBlock);
        Assert.DoesNotContain("roles:", routeBlock);
        Assert.Contains("permissions: ['CanViewPaymentRailCapabilityRegistry', 'CanManageAch', 'CanReadAch']", routeBlock);
        Assert.Contains("Capability Registry multi-riel (solo lectura)", routeBlock);

        Assert.Contains("Consulta administrativa en modo solo lectura para gobernanza multi-riel", pageHtml);
        Assert.Contains("Esta pantalla no permite edición", pageHtml);
        Assert.Contains("Visibilidad read-only del estado efectivo por riel", pageHtml);

        Assert.Contains("this.api.get<", apiService);
        Assert.DoesNotContain("this.api.post<", apiService);
        Assert.DoesNotContain("this.api.put<", apiService);
        Assert.DoesNotContain("this.api.patch<", apiService);
        Assert.DoesNotContain("this.api.delete<", apiService);

        Assert.Contains("[Route(\"api/payment-rails/capability-registry\")]", capabilityController);
        Assert.Contains("[Authorize(Policy = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)]", capabilityController);
        Assert.DoesNotContain("[HttpPost", capabilityController);
        Assert.DoesNotContain("[HttpPut", capabilityController);
        Assert.DoesNotContain("[HttpPatch", capabilityController);
        Assert.DoesNotContain("[HttpDelete", capabilityController);

        var policyBlock = Slice(
            externalDi,
            "options.AddPolicy(FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry",
            "options.AddPolicy(P0Policies.TransactionsRead");
        Assert.Contains("ctx.User.HasClaim(\"permission\", FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)", policyBlock);
        Assert.Contains("ctx.User.HasClaim(\"permission\", \"CanManageAch\")", policyBlock);
        Assert.Contains("ctx.User.HasClaim(\"permission\", \"CanReadAch\")", policyBlock);

        Assert.DoesNotContain("CanViewPaymentRailCapabilityRegistry", permissionConfiguration);
        Assert.Contains("Name = \"CanReadAch\"", permissionConfiguration);
    }

    [Fact]
    public async Task PlatformCapabilities_IsNotMixedWithParametersOrTestToolsAndNavigationLogRemainsUnderSystemRecords()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var administration = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.AdministrationRoute);
        var parameters = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.ParametersAdministrationRoute);
        var platformCapabilities = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.PlatformCapabilitiesRoute);
        var testTools = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.TestToolsRoute);
        var systemRecords = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.SystemRecordsRoute);
        var navigationLog = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.NavigationLogRoute);

        Assert.Equal(administration.Id, platformCapabilities.ParentId);
        Assert.NotEqual(parameters.Id, platformCapabilities.ParentId);
        Assert.NotEqual(testTools.Id, platformCapabilities.ParentId);
        Assert.Equal(systemRecords.Id, navigationLog.ParentId);
        Assert.Equal(30, navigationLog.Order);
    }

    [Fact]
    public async Task CanonicalSeeder_IsIdempotentForNav28EScope()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var seeder = new NavigationInformationArchitectureSeeder(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var first = await ReadNav28ESnapshotAsync(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var second = await ReadNav28ESnapshotAsync(fixture.Context);

        Assert.Equal(first, second);
    }

    private static string Slice(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Start marker not found: {startMarker}");
        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        Assert.True(end > start, $"End marker not found after start: {endMarker}");
        return source[start..end];
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

    private static async Task<string[]> ReadNav28ESnapshotAsync(AchDbContext context)
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
            NavigationInformationArchitectureSeeder.MenuOrganizationRoute,
            NavigationInformationArchitectureSeeder.PlatformCapabilitiesRoute
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
