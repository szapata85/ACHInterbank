using System.Reflection;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.External;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav32BTestToolsPermissionEnforcementTests
{
    [Fact]
    public async Task MenuAuthorization_RequiresOnlyTestToolsAccess_ForCanonicalDomainAndTools_Idempotently()
    {
        await using var fixture = CreateFixture();
        await EnsureServiceTestsMenuItemAsync(fixture.Context);

        var permissionSeeder = new TestToolsPermissionSeeder(fixture.Context);
        var grantSeeder = new TestToolsAdminRolePermissionSeeder(fixture.Context);
        var menuAuthorizationSeeder = new TestToolsMenuAuthorizationSeeder(fixture.Context);

        Assert.True(permissionSeeder.Order < grantSeeder.Order);
        Assert.True(grantSeeder.Order < NavigationInformationArchitectureSeeder.FinalReconciliationOrder);
        Assert.True(NavigationInformationArchitectureSeeder.FinalReconciliationOrder < menuAuthorizationSeeder.Order);

        await permissionSeeder.SeedAsync();
        await grantSeeder.SeedAsync();
        await menuAuthorizationSeeder.SeedAsync();
        await menuAuthorizationSeeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var targets = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.Id == MenuItemConfiguration.UatSimulatorsId
                || item.Id == MenuItemConfiguration.NachaInboundSimulatorId
                || item.Route == NavigationInformationArchitectureSeeder.ServiceTestsRoute)
            .OrderBy(item => item.Route)
            .ToArrayAsync();

        Assert.Equal(3, targets.Length);
        var targetIds = targets.Select(item => item.Id).ToArray();

        var permissionLinks = await fixture.Context.MenuItemPermissions
            .AsNoTracking()
            .Where(link => targetIds.Contains(link.MenuItemId))
            .OrderBy(link => link.MenuItemId)
            .ThenBy(link => link.PermissionId)
            .ToArrayAsync();

        Assert.Equal(3, permissionLinks.Length);
        Assert.All(permissionLinks, link => Assert.Equal(TestToolsPermissionSeeder.AccessPermissionId, link.PermissionId));
        Assert.Equal(3, permissionLinks.Select(link => link.MenuItemId).Distinct().Count());

        var roleLinks = await fixture.Context.MenuItemRoles
            .AsNoTracking()
            .Where(link => targetIds.Contains(link.MenuItemId))
            .ToArrayAsync();
        Assert.Empty(roleLinks);

        var serviceTests = Assert.Single(targets, item => item.Route == NavigationInformationArchitectureSeeder.ServiceTestsRoute);
        Assert.True(serviceTests.IsActive);

        var testToolsGrants = await fixture.Context.RolePermissions
            .AsNoTracking()
            .Where(link => link.PermissionId == TestToolsPermissionSeeder.AccessPermissionId)
            .ToArrayAsync();
        var adminGrant = Assert.Single(testToolsGrants);
        Assert.Equal(RoleConfiguration.AdminRoleId, adminGrant.RoleId);
        Assert.DoesNotContain(testToolsGrants, link => link.RoleId == RoleConfiguration.OperatorRoleId);
    }

    [Fact]
    public async Task TestToolsAccessPolicy_DoesNotAcceptLegacyAchPermissionsAsFallback()
    {
        using var provider = BuildExternalProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        var legacyOnly = Principal("CanReadAch", "CanManageAch", FineGrainedPermissions.NachaSimulator.Read);
        var dedicated = Principal(FineGrainedPermissions.TestTools.Access);
        var currentAdminCapabilities = Principal(FineGrainedPermissions.TestTools.Access, "CanReadAch", "CanManageAch");

        // Legacy ACH/simulator permissions can still satisfy the action-specific compatibility policies,
        // but they cannot satisfy the mandatory TestTools.Access domain policy.
        Assert.True((await authorization.AuthorizeAsync(legacyOnly, resource: null, P1Policies.NachaSimulatorRead)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(legacyOnly, resource: null, P1Policies.NachaSimulatorGenerateIncoming)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(legacyOnly, resource: null, FineGrainedPermissions.TestTools.Access)).Succeeded);

        Assert.True((await authorization.AuthorizeAsync(dedicated, resource: null, FineGrainedPermissions.TestTools.Access)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(currentAdminCapabilities, resource: null, FineGrainedPermissions.TestTools.Access)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(currentAdminCapabilities, resource: null, P1Policies.NachaSimulatorRead)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(currentAdminCapabilities, resource: null, P1Policies.NachaSimulatorGenerateIncoming)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(currentAdminCapabilities, resource: null, P1Policies.NachaSimulatorGenerateDifferential)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(currentAdminCapabilities, resource: null, P1Policies.NachaSimulatorDownload)).Succeeded);
    }

    [Fact]
    public void TestToolControllers_RequireDedicatedAccess_WhileSpecificPoliciesRemainAdditional()
    {
        AssertControllerGate<NachaInboundSimulatorController>();
        AssertControllerGate<NachaSoapUatConsoleController>();

        var inboundReadActions = new[]
        {
            nameof(NachaInboundSimulatorController.List),
            nameof(NachaInboundSimulatorController.GetById),
            nameof(NachaInboundSimulatorController.Evidence),
            nameof(NachaInboundSimulatorController.Preview),
            nameof(NachaInboundSimulatorController.EligibleDifferentialTransactions),
            nameof(NachaInboundSimulatorController.AvailableCycles)
        };
        foreach (var actionName in inboundReadActions)
        {
            AssertActionPolicy<NachaInboundSimulatorController>(actionName, P1Policies.NachaSimulatorRead);
        }
        AssertActionPolicy<NachaInboundSimulatorController>(nameof(NachaInboundSimulatorController.DownloadFile), P1Policies.NachaSimulatorDownload);

        foreach (var actionName in new[]
        {
            nameof(NachaSoapUatConsoleController.GetDashboard),
            nameof(NachaSoapUatConsoleController.GetCandidates),
            nameof(NachaSoapUatConsoleController.GetCandidate),
            nameof(NachaSoapUatConsoleController.GetAudit)
        })
        {
            var method = typeof(NachaSoapUatConsoleController).GetMethod(actionName)
                ?? throw new InvalidOperationException($"Action {actionName} not found.");
            Assert.Empty(method.GetCustomAttributes<AuthorizeAttribute>(inherit: true));
        }
    }

    [Fact]
    public async Task SpaRoutes_RequireTestToolsAccess_AndServiceTestsAreDecoupledFromAchParent()
    {
        var root = RepositoryRoot();
        var appRouting = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "app-routing.module.ts"));
        var uatRouting = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "features", "uat", "uat-routing.module.ts"));
        var operationalRouting = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "features", "nacha-operational", "nacha-operational-routing.module.ts"));
        var menuHandler = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Application", "Navigation", "Queries", "GetMenuForCurrentUserHandler.cs"));
        var inboundController = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Api", "Controllers", "NachaInboundSimulatorController.cs"));
        var inboundService = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "ACH", "Services", "Implementation", "NachaInboundSimulationService.cs"));
        var soapController = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Api", "Controllers", "NachaSoapUatConsoleController.cs"));
        var uatContrapartidasController = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Api", "Controllers", "UatContrapartidasController.cs"));

        var uatBlock = Slice(appRouting, "path: 'uat'", "path: 'incoming-nacha-command-center'");
        Assert.Contains("canActivate: [permissionGuard]", uatBlock);
        Assert.Contains("permissions: ['TestTools.Access']", uatBlock);
        Assert.DoesNotContain("roles:", uatBlock);
        Assert.DoesNotContain("CanReadAch", uatBlock);
        Assert.DoesNotContain("CanManageAch", uatBlock);

        Assert.Contains("permissions: ['TestTools.Access']", uatRouting);
        Assert.DoesNotContain("permissions: ['CanManageAch']", uatRouting);

        var soapRoute = Slice(appRouting, "path: 'ach/nacha/soap-uat-console'", "path: 'ach'");
        Assert.Contains("canActivate: [permissionGuard]", soapRoute);
        Assert.Contains("permissions: ['TestTools.Access']", soapRoute);
        Assert.DoesNotContain("roles:", soapRoute);
        Assert.DoesNotContain("CanReadAch", soapRoute);
        Assert.DoesNotContain("CanManageAch", soapRoute);
        Assert.DoesNotContain("nacha/soap-uat-console", operationalRouting);

        // NAV3.3A keeps the inbound simulator safe; NAV3.3B removes only the service-console environment gate.
        Assert.DoesNotContain("IsProduction", menuHandler);
        Assert.DoesNotContain("IsUatLike", menuHandler);
        Assert.DoesNotContain("IsProduction", inboundController);
        Assert.DoesNotContain("IsUatLike", inboundController);
        Assert.DoesNotContain("if (!_options.IsUatLike())", inboundService);
        Assert.Contains("if (!_options.Enabled)", inboundService);
        Assert.Contains("_options.AllowAutoImport || _options.AllowExternalTransmission", inboundService);
        Assert.DoesNotContain("IsProduction", soapController);
        Assert.DoesNotContain("IsUatLike", soapController);
        Assert.DoesNotContain("NachaSimulatorRead", soapController);
        Assert.Contains("_hostEnvironment.IsProduction()", uatContrapartidasController);
        Assert.Contains("ACH_SOAP_LIVE_TESTS", uatContrapartidasController);
    }

    private static void AssertControllerGate<TController>()
    {
        var policies = typeof(TController)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Select(attribute => attribute.Policy)
            .ToArray();
        Assert.Contains(FineGrainedPermissions.TestTools.Access, policies);
    }

    private static void AssertActionPolicy<TController>(string actionName, string expectedPolicy)
    {
        var method = typeof(TController).GetMethod(actionName)
            ?? throw new InvalidOperationException($"Action {typeof(TController).Name}.{actionName} not found.");
        Assert.Contains(method.GetCustomAttributes<AuthorizeAttribute>(inherit: true), attribute => attribute.Policy == expectedPolicy);
    }

    private static ClaimsPrincipal Principal(params string[] permissions)
        => new(new ClaimsIdentity(
            permissions.Select(permission => new Claim("permission", permission)),
            authenticationType: "test"));

    private static ServiceProvider BuildExternalProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["appSettings:tokenManager:issuerJwt"] = "issuer-test",
                ["appSettings:tokenManager:audienceJwt"] = "audience-test",
                ["appSettings:tokenManager:secretKetJwt"] = "this-is-a-test-secret-key-with-32-bytes"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddExternal(config);
        return services.BuildServiceProvider();
    }

    private static async Task EnsureServiceTestsMenuItemAsync(AchDbContext context)
    {
        if (!await context.MenuItems.AnyAsync(item => item.Route == NavigationInformationArchitectureSeeder.ServiceTestsRoute))
        {
            context.MenuItems.Add(new MenuItem
            {
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = MenuItemConfiguration.UatSimulatorsId,
                Label = NavigationInformationArchitectureSeeder.ServiceTestsLabel,
                Route = NavigationInformationArchitectureSeeder.ServiceTestsRoute,
                Icon = "science",
                Order = 20,
                Exact = true,
                IsActive = false
            });
            await context.SaveChangesAsync();
        }

        var serviceTests = await context.MenuItems.SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.ServiceTestsRoute);
        if (!await context.MenuItemPermissions.AnyAsync(link => link.MenuItemId == serviceTests.Id))
        {
            context.MenuItemPermissions.Add(new MenuItemPermission
            {
                MenuItemId = serviceTests.Id,
                PermissionId = PermissionConfiguration.ManageAchPermissionId
            });
        }
        if (!await context.MenuItemRoles.AnyAsync(link => link.MenuItemId == serviceTests.Id))
        {
            context.MenuItemRoles.AddRange(
                new MenuItemRole { MenuItemId = serviceTests.Id, RoleId = RoleConfiguration.AdminRoleId },
                new MenuItemRole { MenuItemId = serviceTests.Id, RoleId = RoleConfiguration.OperatorRoleId });
        }
        await context.SaveChangesAsync();
    }

    private static string Slice(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Start marker not found: {startMarker}");
        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        Assert.True(end > start, $"End marker not found after start: {endMarker}");
        return source[start..end];
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
