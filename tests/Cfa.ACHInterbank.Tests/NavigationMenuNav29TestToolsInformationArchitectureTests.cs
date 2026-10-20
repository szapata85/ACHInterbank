using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav29TestToolsInformationArchitectureTests
{
    [Fact]
    public async Task CanonicalSeeder_CanonicalizesRealTestToolSurfacesWithoutPublishingBlockedServiceConsole()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        Assert.False(await fixture.Context.MenuItems
            .AsNoTracking()
            .AnyAsync(item => item.Route == NavigationInformationArchitectureSeeder.ServiceTestsRoute));

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var testTools = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.UatSimulatorsId);
        Assert.Equal(NavigationInformationArchitectureSeeder.TestToolsRoute, testTools.Route);
        Assert.Equal(NavigationInformationArchitectureSeeder.TestToolsLabel, testTools.Label);
        Assert.Null(testTools.ParentId);
        Assert.Equal(90, testTools.Order);
        Assert.True(testTools.IsActive);

        var inboundSimulator = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.NachaInboundSimulatorId);
        Assert.Equal(NavigationInformationArchitectureSeeder.InboundFileSimulatorRoute, inboundSimulator.Route);
        Assert.Equal(NavigationInformationArchitectureSeeder.InboundFileSimulatorLabel, inboundSimulator.Label);
        Assert.Equal(testTools.Id, inboundSimulator.ParentId);
        Assert.Equal(10, inboundSimulator.Order);
        Assert.True(inboundSimulator.IsActive);

        var serviceTests = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.ServiceTestsRoute);
        Assert.Equal(NavigationInformationArchitectureSeeder.ServiceTestsLabel, serviceTests.Label);
        Assert.Equal(testTools.Id, serviceTests.ParentId);
        Assert.Equal(20, serviceTests.Order);
        Assert.True(serviceTests.Exact);
        Assert.False(serviceTests.IsActive); // NAV3 must replace environment gating and establish the dedicated technical permission first.

        var children = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.ParentId == testTools.Id)
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order, item.IsActive })
            .ToArrayAsync();

        Assert.Equal(
            new[]
            {
                (Route: "/uat/nacha-inbound-simulator", Label: "Simulador de archivos recibidos", Order: 10, IsActive: true),
                (Route: "/ach/nacha/soap-uat-console", Label: "Pruebas de servicios", Order: 20, IsActive: false)
            },
            children.Select(item => (item.Route, item.Label, item.Order, item.IsActive)).ToArray());

        var administration = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.AdministrationRoute);
        Assert.Equal(80, administration.Order);

        var administrationChildren = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == administration.Id)
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Order })
            .ToArrayAsync();
        Assert.Equal(
            new[]
            {
                (Route: "/users", Order: 10),
                (Route: "/scheduler/tasks", Order: 20),
                (Route: "/logs", Order: 30),
                (Route: "/navigation/menu-items", Order: 40),
                (Route: "/payment-rail-capability-registry", Order: 50)
            },
            administrationChildren.Select(item => (item.Route, item.Order)).ToArray());
    }

    [Fact]
    public async Task TestToolsAuthorization_PreservesExistingLinksAndDoesNotChangePermissionRoleOrGrantDefinitions()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var existingIds = new[]
        {
            MenuItemConfiguration.UatSimulatorsId,
            MenuItemConfiguration.NachaInboundSimulatorId
        };
        var permissionLinksBefore = await ReadPermissionLinksAsync(fixture.Context, existingIds);
        var roleLinksBefore = await ReadRoleLinksAsync(fixture.Context, existingIds);
        var permissionDefinitionsBefore = await ReadPermissionDefinitionsAsync(fixture.Context);
        var roleDefinitionsBefore = await ReadRoleDefinitionsAsync(fixture.Context);
        var roleGrantsBefore = await ReadRoleGrantsAsync(fixture.Context);

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        Assert.Equal(permissionLinksBefore, await ReadPermissionLinksAsync(fixture.Context, existingIds));
        Assert.Equal(roleLinksBefore, await ReadRoleLinksAsync(fixture.Context, existingIds));
        Assert.Equal(permissionDefinitionsBefore, await ReadPermissionDefinitionsAsync(fixture.Context));
        Assert.Equal(roleDefinitionsBefore, await ReadRoleDefinitionsAsync(fixture.Context));
        Assert.Equal(roleGrantsBefore, await ReadRoleGrantsAsync(fixture.Context));

        var serviceTests = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.ServiceTestsRoute);
        Assert.False(serviceTests.IsActive);
        Assert.Equal(
            new[] { $"{serviceTests.Id}|{PermissionConfiguration.ManageAchPermissionId}" },
            await ReadPermissionLinksAsync(fixture.Context, [serviceTests.Id]));
        Assert.Equal(
            new[]
            {
                $"{serviceTests.Id}|{RoleConfiguration.AdminRoleId}",
                $"{serviceTests.Id}|{RoleConfiguration.OperatorRoleId}"
            },
            await ReadRoleLinksAsync(fixture.Context, [serviceTests.Id]));
    }

    [Fact]
    public async Task CurrentCode_ConfirmsTwoSpaTestToolSurfacesWithIndependentServiceToolAuthorization()
    {
        var root = RepositoryRoot();
        var appRouting = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "app-routing.module.ts"));
        var uatRouting = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "features", "uat", "uat-routing.module.ts"));
        var operationalRouting = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "features", "nacha-operational", "nacha-operational-routing.module.ts"));
        var simulatorHtml = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "features", "uat", "components", "nacha-inbound-simulator", "nacha-inbound-simulator.component.html"));
        var serviceConsoleHtml = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "features", "nacha-operational", "pages", "nacha-soap-uat-console.component.html"));
        var menuHandler = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Application", "Navigation", "Queries", "GetMenuForCurrentUserHandler.cs"));
        var simulatorController = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Api", "Controllers", "NachaInboundSimulatorController.cs"));
        var serviceConsoleController = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Api", "Controllers", "NachaSoapUatConsoleController.cs"));
        var uatContrapartidasController = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Api", "Controllers", "UatContrapartidasController.cs"));
        var testsController = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Api", "Controllers", "TestsController.cs"));
        var seeder = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "Navigation", "Seeders", "NavigationInformationArchitectureSeeder.cs"));

        Assert.Contains("path: 'uat'", appRouting);
        var uatBlock = Slice(appRouting, "path: 'uat'", "path: 'incoming-nacha-command-center'");
        Assert.Contains("canActivate: [permissionGuard]", uatBlock);
        Assert.Contains("permissions: ['TestTools.Access']", uatBlock);
        Assert.DoesNotContain("roles:", uatBlock);
        Assert.DoesNotContain("CanManageAch", uatBlock);
        Assert.DoesNotContain("CanReadAch", uatBlock);
        Assert.Contains("path: 'nacha-inbound-simulator'", uatRouting);
        Assert.Contains("permissions: ['TestTools.Access']", uatRouting);
        Assert.Contains("Simular respuesta de otra entidad", simulatorHtml);
        Assert.Contains("Generación sin transmisión automática", simulatorHtml);

        Assert.DoesNotContain("path: 'nacha/soap-uat-console'", operationalRouting);
        var serviceTestsBlock = Slice(appRouting, "path: 'ach/nacha/soap-uat-console'", "path: 'ach'");
        Assert.Contains("permissions: ['TestTools.Access']", serviceTestsBlock);
        Assert.DoesNotContain("roles:", serviceTestsBlock);
        Assert.DoesNotContain("CanManageAch", serviceTestsBlock);
        Assert.DoesNotContain("CanReadAch", serviceTestsBlock);
        Assert.Contains("Consola SOAP/UAT solo lectura", serviceConsoleHtml);
        Assert.Contains("La consola no ejecuta SOAP", serviceConsoleHtml);
        Assert.Contains("[Route(\"api/ach/nacha/soap-uat-console\")]", serviceConsoleController);
        Assert.Contains("[Authorize(Policy = FineGrainedPermissions.TestTools.Access)]", serviceConsoleController);
        Assert.DoesNotContain("NachaSimulatorRead", serviceConsoleController);
        Assert.Contains("[Authorize(Policy = FineGrainedPermissions.TestTools.Access)]", simulatorController);

        // Backend-only technical endpoints are real, but there is no independent SPA navigation surface for them.
        Assert.Contains("[Route(\"api/uat/contrapartidas\")]", uatContrapartidasController);
        Assert.Contains("[Route(\"/[controller]\")]", testsController);
        Assert.DoesNotContain("path: 'contrapartidas'", appRouting);
        Assert.DoesNotContain("path: 'tests'", appRouting, StringComparison.OrdinalIgnoreCase);

        // NAV3.3A removed inbound-simulator environment gating; NAV3.3B removes the read-only service-console gate only.
        Assert.DoesNotContain("IsProduction", menuHandler);
        Assert.DoesNotContain("IsUatLike", menuHandler);
        Assert.DoesNotContain("IsProduction", simulatorController);
        Assert.DoesNotContain("IsUatLike", simulatorController);
        Assert.DoesNotContain("IsProduction", serviceConsoleController);
        Assert.DoesNotContain("IsUatLike", serviceConsoleController);
        Assert.Contains("_hostEnvironment.IsProduction()", uatContrapartidasController);
        Assert.DoesNotContain("IsProduction", seeder);
        Assert.DoesNotContain("IsUatLike", seeder);
    }

    [Fact]
    public async Task CanonicalSeeder_IsIdempotentForNav29Scope()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var seeder = new NavigationInformationArchitectureSeeder(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var first = await ReadNav29SnapshotAsync(fixture.Context);

        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var second = await ReadNav29SnapshotAsync(fixture.Context);

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

    private static async Task<string[]> ReadNav29SnapshotAsync(AchDbContext context)
    {
        var routes = new[]
        {
            NavigationInformationArchitectureSeeder.TestToolsRoute,
            NavigationInformationArchitectureSeeder.InboundFileSimulatorRoute,
            NavigationInformationArchitectureSeeder.ServiceTestsRoute
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
