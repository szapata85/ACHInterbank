using System.Reflection;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.DataBase.Queries.Navigation;
using Cfa.ACHInterbank.Application.Navigation;
using Cfa.ACHInterbank.Application.Navigation.Queries;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.External;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav34RIntegrationsReadAudienceTests
{
    private static readonly string[] IntegrationChildRoutes =
    [
        IntegrationContractMenuAuthorizationSeeder.Route,
        InformationRelationshipMenuAuthorizationSeeder.Route,
        ParticipantConnectionMenuAuthorizationSeeder.Route
    ];

    [Fact]
    public async Task IntegrationsRead_IsPreExistingCapability_MaterializedIdempotentlyWithoutGrants()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        var grantsBefore = await RoleGrantsAsync(context);

        Assert.Contains(FineGrainedPermissions.Integrations.Read, FineGrainedPermissions.AllPermissions);

        var seeder = new IntegrationReadPermissionSeeder(context);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var permission = await context.Permissions.AsNoTracking()
            .SingleAsync(item => item.Name == FineGrainedPermissions.Integrations.Read);
        Assert.Equal(IntegrationReadPermissionSeeder.ReadPermissionId, permission.Id);
        Assert.Equal("Consultar configuración de integraciones", permission.Description);
        Assert.Empty(await context.RolePermissions.AsNoTracking()
            .Where(link => link.PermissionId == permission.Id)
            .ToArrayAsync());
        Assert.Equal(grantsBefore, await RoleGrantsAsync(context));
    }

    [Fact]
    public async Task IntegrationsReadPolicy_AllowsFineCapabilityOrLegacyRead_ButNotManagement()
    {
        using var provider = BuildExternalProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Integrations.Read), null, P1Policies.IntegrationsRead)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(
            Principal("CanReadAch"), null, P1Policies.IntegrationsRead)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal("CanManageAch"), null, P1Policies.IntegrationsRead)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Config.Read), null, P1Policies.IntegrationsRead)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Integrations.ManageContracts), null, P1Policies.IntegrationsRead)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Integrations.ManageMappings), null, P1Policies.IntegrationsRead)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Integrations.ManageConnections), null, P1Policies.IntegrationsRead)).Succeeded);
    }

    [Fact]
    public async Task NavigationAudienceSeeder_AlignsThreeIntegrationChildren_AndRemovesObsoleteParentCanManageUsers()
    {
        await using var fixture = CreateFixture();
        await SeedThroughNav34QAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var grantsBefore = await RoleGrantsAsync(fixture.Context);
        var mftBefore = await AuthorizationSnapshotAsync(fixture.Context, AchColombiaMftAdministrationMenuSeeder.Route);

        await new IntegrationReadPermissionSeeder(fixture.Context).SeedAsync();
        var seeder = new IntegrationReadNavigationAudienceSeeder(fixture.Context);
        Assert.True(ParticipantConnectionMenuAuthorizationSeeder.AuthorizationReconciliationOrder < seeder.Order);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var connections = await fixture.Context.MenuItems.AsNoTracking()
            .SingleAsync(item => item.Route == ConnectionsStructuralMenuAuthorizationSeeder.ConnectionsRoute);
        Assert.Equal(MenuItemConfiguration.IntegrationsId, connections.Id);
        Assert.Equal("Conexiones", connections.Label);
        Assert.Equal(80, connections.Order);
        Assert.True(connections.IsActive);
        Assert.Empty(await RoleNamesAsync(fixture.Context, connections.Id));
        Assert.Equal(
            ["CanReadAch", FineGrainedPermissions.Config.Read, FineGrainedPermissions.Integrations.Read],
            (await PermissionNamesAsync(fixture.Context, connections.Id)).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        Assert.DoesNotContain("CanManageUsers", await PermissionNamesAsync(fixture.Context, connections.Id));

        foreach (var route in IntegrationChildRoutes)
        {
            var child = await fixture.Context.MenuItems.AsNoTracking().SingleAsync(item => item.Route == route);
            Assert.Equal(
                ["CanReadAch", FineGrainedPermissions.Integrations.Read],
                (await PermissionNamesAsync(fixture.Context, child.Id)).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Assert.Equal(
                ["ACH.Operator", "Admin"],
                (await RoleNamesAsync(fixture.Context, child.Id)).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        Assert.Equal(mftBefore, await AuthorizationSnapshotAsync(fixture.Context, AchColombiaMftAdministrationMenuSeeder.Route));
        Assert.Equal(grantsBefore, await RoleGrantsAsync(fixture.Context));
    }

    [Fact]
    public async Task MenuFiltering_AdminAndOperatorWithLegacyRead_SeeTheSameIntegrationChildren()
    {
        await using var fixture = CreateFixture();
        var repository = await SeedAndBuildRepositoryAsync(fixture.Context);

        foreach (var role in new[] { "Admin", "ACH.Operator" })
        {
            var menu = await MenuFor(repository,
            [
                new Claim(ClaimTypes.Role, role),
                new Claim("permission", "CanReadAch")
            ]);

            var connections = FindByRoute(menu, "/integraciones");
            Assert.NotNull(connections);
            foreach (var route in IntegrationChildRoutes)
                Assert.Contains(connections!.Children, item => item.Route == route);
        }
    }

    [Fact]
    public async Task MenuFiltering_FineReadRequiresApprovedRoleForIntegrationChildren_AndDoesNotGrantMft()
    {
        await using var fixture = CreateFixture();
        var repository = await SeedAndBuildRepositoryAsync(fixture.Context);

        foreach (var role in new[] { "Admin", "ACH.Operator" })
        {
            var menu = await MenuFor(repository,
            [
                new Claim(ClaimTypes.Role, role),
                new Claim("permission", FineGrainedPermissions.Integrations.Read)
            ]);

            var connections = FindByRoute(menu, "/integraciones");
            Assert.NotNull(connections);
            Assert.Equal(IntegrationChildRoutes, connections!.Children.Select(item => item.Route).ToArray());
            Assert.DoesNotContain(connections.Children, item => item.Route == AchColombiaMftAdministrationMenuSeeder.Route);
        }

        var noRoleMenu = await MenuFor(repository,
        [
            new Claim("permission", FineGrainedPermissions.Integrations.Read)
        ]);
        var noRoleConnections = FindByRoute(noRoleMenu, "/integraciones");
        Assert.NotNull(noRoleConnections);
        Assert.Empty(noRoleConnections!.Children);
    }

    [Fact]
    public async Task MenuFiltering_RoleWithoutReadOrCanManageUsersOnly_DoesNotExposeIntegrationChildren()
    {
        await using var fixture = CreateFixture();
        var repository = await SeedAndBuildRepositoryAsync(fixture.Context);

        foreach (var claims in new[]
        {
            new[] { new Claim(ClaimTypes.Role, "Admin") },
            new[] { new Claim(ClaimTypes.Role, "ACH.Operator") },
            new[] { new Claim("permission", "CanManageUsers") }
        })
        {
            var routes = Flatten(await MenuFor(repository, claims)).Select(item => item.Route).ToArray();
            Assert.DoesNotContain("/integraciones/contrato", routes);
            Assert.DoesNotContain("/integraciones/relacion-informacion", routes);
            Assert.DoesNotContain("/integraciones/conexion", routes);
        }
    }

    [Fact]
    public void IntegrationReadApiPolicy_CoversIntegrationGetEndpoints_AndMutationPoliciesRemainSeparated()
    {
        AssertActionPolicy<IntegrationCatalogController>(nameof(IntegrationCatalogController.GetMethods), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy<IntegrationCatalogController>(nameof(IntegrationCatalogController.GetParametersByMethod), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy<IntegrationCatalogController>(nameof(IntegrationCatalogController.GetSourceCatalog), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy<IntegrationCatalogController>(nameof(IntegrationCatalogController.GetTransformations), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);

        AssertActionPolicy<IntegrationContractsController>(nameof(IntegrationContractsController.GetByMethod), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy<IntegrationContractsController>(nameof(IntegrationContractsController.GetById), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy<IntegrationContractsController>(nameof(IntegrationContractsController.Compare), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy<IntegrationContractsController>(nameof(IntegrationContractsController.UpdatePresentation), typeof(HttpPutAttribute), P1Policies.IntegrationContractsManage);
        AssertActionPolicy<IntegrationContractsController>(nameof(IntegrationContractsController.AnalyzeArtifact), typeof(HttpPostAttribute), P1Policies.IntegrationContractsManage);
        AssertActionPolicy<IntegrationContractsController>(nameof(IntegrationContractsController.PreviewArtifact), typeof(HttpPostAttribute), P1Policies.IntegrationContractsManage);
        AssertActionPolicy<IntegrationContractsController>(nameof(IntegrationContractsController.ImportArtifact), typeof(HttpPostAttribute), P1Policies.IntegrationContractsManage);

        AssertActionPolicy<IntegrationMappingSetsController>(nameof(IntegrationMappingSetsController.Get), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy<IntegrationMappingSetsController>(nameof(IntegrationMappingSetsController.GetPublished), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy<IntegrationMappingSetsController>(nameof(IntegrationMappingSetsController.GetById), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy<IntegrationMappingSetsController>(nameof(IntegrationMappingSetsController.GetWorkspace), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy<IntegrationMappingSetsController>(nameof(IntegrationMappingSetsController.History), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy<IntegrationMappingSetsController>(nameof(IntegrationMappingSetsController.Create), typeof(HttpPostAttribute), P1Policies.IntegrationMappingsManage);
        AssertActionPolicy<IntegrationMappingSetsController>(nameof(IntegrationMappingSetsController.Validate), typeof(HttpPostAttribute), P1Policies.IntegrationMappingsValidate);
        AssertActionPolicy<IntegrationMappingSetsController>(nameof(IntegrationMappingSetsController.Publish), typeof(HttpPostAttribute), P1Policies.IntegrationMappingsPublish);

        AssertActionPolicy<SoapIntegrationSettingsController>(nameof(SoapIntegrationSettingsController.GetAsync), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy<SoapIntegrationSettingsController>(nameof(SoapIntegrationSettingsController.PreviewChangeAsync), typeof(HttpPostAttribute), P1Policies.IntegrationConnectionsManage);
        AssertActionPolicy<SoapIntegrationSettingsController>(nameof(SoapIntegrationSettingsController.SaveAsync), typeof(HttpPutAttribute), P1Policies.IntegrationConnectionsManage);
    }

    [Fact]
    public async Task SourceRegression_ParentAndChildrenUseTransitionalRead_MftAndMutationPoliciesRemainIntact()
    {
        var root = RepositoryRoot();
        var appRouting = await Read(root, "web/ach-interbank-ui/src/app/app-routing.module.ts");
        var childRouting = await Read(root, "web/ach-interbank-ui/src/app/features/integrations/integrations.routes.ts");
        var contractStore = await Read(root, "web/ach-interbank-ui/src/app/features/integrations/state/contract-workspace.store.ts");
        var connectionComponent = await Read(root, "web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.ts");
        var mft = await Read(root, "web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.ts");

        Assert.Contains("roles: ['Admin', 'ACH.Operator']", Slice(appRouting, "path: 'integraciones'", "path: 'navigation'"));
        Assert.Contains("permissions: ['Integrations.Read', 'CanReadAch', 'CanManageAch']", Slice(appRouting, "path: 'integraciones'", "path: 'navigation'"));
        Assert.Equal(4, Count(childRouting, "permissions: ['Integrations.Read', 'CanReadAch']"));
        Assert.Contains("permissions.includes('Integrations.Read') || permissions.includes('CanReadAch')", contractStore);
        Assert.Contains("hasPermission(['Integrations.Read', 'CanReadAch'])", connectionComponent);

        Assert.Contains("permissions: ['Config.Read', 'CanReadAch']", Slice(appRouting, "path: 'administracion/mft-ach'", "path: 'cenit'"));
        Assert.Contains("hasPermission(['Config.Manage', 'CanManageAch'])", mft);

        Assert.Contains("Integrations.ManageContracts", contractStore);
        Assert.Contains("Integrations.ManageConnections", connectionComponent);
    }

    private static void AssertActionPolicy<TController>(string methodName, Type httpMethodAttribute, string expectedPolicy)
    {
        var action = typeof(TController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(action);
        Assert.NotNull(action!.GetCustomAttribute(httpMethodAttribute));
        var authorize = action.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(expectedPolicy, authorize!.Policy);
    }

    private static async Task<StaticMenuRepository> SeedAndBuildRepositoryAsync(AchDbContext context)
    {
        await SeedThroughNav34QAsync(context);
        await new IntegrationReadPermissionSeeder(context).SeedAsync();
        await new IntegrationReadNavigationAudienceSeeder(context).SeedAsync();
        context.ChangeTracker.Clear();

        var relevantRoutes = new HashSet<string>(StringComparer.Ordinal)
        {
            NavigationInformationArchitectureSeeder.ParametersAdministrationRoute,
            "/integraciones",
            "/integraciones/contrato",
            "/integraciones/relacion-informacion",
            "/integraciones/conexion",
            AchColombiaMftAdministrationMenuSeeder.Route
        };

        var items = await context.MenuItems.AsNoTracking()
            .Include(item => item.MenuItemRoles).ThenInclude(link => link.Role)
            .Include(item => item.MenuItemPermissions).ThenInclude(link => link.Permission)
            .Where(item => relevantRoutes.Contains(item.Route))
            .ToListAsync();

        return new StaticMenuRepository(items);
    }

    private static async Task SeedThroughNav34QAsync(AchDbContext context)
    {
        IDbSeeder[] functionalSeeders =
        [
            new ClaimsPermissionSeeder(context),
            new CfaOperationalTransactionRestrictionsPermissionSeeder(context),
            new ConfigurationPermissionSeeder(context),
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

        foreach (var seeder in functionalSeeders.OrderBy(item => item.Order).ThenBy(item => item.GetType().FullName, StringComparer.Ordinal))
            await seeder.SeedAsync();

        await new NavigationInformationArchitectureSeeder(context).SeedAsync();
        await new MftAdministrationMenuAuthorizationSeeder(context).SeedAsync();
        await new ConnectionsStructuralMenuAuthorizationSeeder(context).SeedAsync();
        await new IntegrationContractMenuAuthorizationSeeder(context).SeedAsync();
        await new InformationRelationshipMenuAuthorizationSeeder(context).SeedAsync();
        await new ParticipantConnectionMenuAuthorizationSeeder(context).SeedAsync();
    }

    private static async Task<IList<MenuItemDto>> MenuFor(StaticMenuRepository repository, IEnumerable<Claim> claims)
        => await new GetMenuForCurrentUserHandler(repository, new TestCurrentUserContext(claims))
            .Handle(new GetMenuForCurrentUserQuery(), CancellationToken.None);

    private static IEnumerable<MenuItemDto> Flatten(IEnumerable<MenuItemDto> items)
    {
        foreach (var item in items)
        {
            yield return item;
            foreach (var child in Flatten(item.Children))
                yield return child;
        }
    }

    private static MenuItemDto? FindByRoute(IEnumerable<MenuItemDto> items, string route)
        => Flatten(items).FirstOrDefault(item => item.Route == route);

    private static ServiceProvider BuildExternalProvider()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["appSettings:tokenManager:issuerJwt"] = "issuer-test",
            ["appSettings:tokenManager:audienceJwt"] = "audience-test",
            ["appSettings:tokenManager:secretKetJwt"] = "this-is-a-test-secret-key-with-32-bytes"
        }).Build();
        services.AddExternal(config);
        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal Principal(params string[] permissions)
        => new(new ClaimsIdentity(permissions.Select(permission => new Claim("permission", permission)), "test"));

    private static async Task<string[]> PermissionNamesAsync(AchDbContext context, int menuItemId)
        => await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == menuItemId)
            .Join(context.Permissions.AsNoTracking(), link => link.PermissionId, permission => permission.Id, (_, permission) => permission.Name)
            .OrderBy(name => name)
            .ToArrayAsync();

    private static async Task<string[]> RoleNamesAsync(AchDbContext context, int menuItemId)
        => await context.MenuItemRoles.AsNoTracking()
            .Where(link => link.MenuItemId == menuItemId)
            .Join(context.Roles.AsNoTracking(), link => link.RoleId, role => role.Id, (_, role) => role.Name)
            .OrderBy(name => name)
            .ToArrayAsync();

    private static async Task<string> AuthorizationSnapshotAsync(AchDbContext context, string route)
    {
        var id = await context.MenuItems.AsNoTracking().Where(item => item.Route == route).Select(item => item.Id).SingleAsync();
        var permissions = await PermissionNamesAsync(context, id);
        var roles = await RoleNamesAsync(context, id);
        return $"{route}|R={string.Join(',', roles)}|P={string.Join(',', permissions)}";
    }

    private static async Task<string[]> RoleGrantsAsync(AchDbContext context)
        => await context.RolePermissions.AsNoTracking()
            .OrderBy(link => link.RoleId)
            .ThenBy(link => link.PermissionId)
            .Select(link => $"{link.RoleId:D}|{link.PermissionId:D}")
            .ToArrayAsync();

    private static Fixture CreateFixture()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        return new Fixture(context, connection);
    }

    private static async Task<string> Read(string root, string relativePath)
        => await File.ReadAllTextAsync(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string Slice(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Start marker not found: {startMarker}");
        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        Assert.True(end > start, $"End marker not found after start: {endMarker}");
        return source[start..end];
    }

    private static int Count(string source, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }
        return count;
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ACHInterbank.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private sealed class StaticMenuRepository(IReadOnlyCollection<MenuItem> items) : IMenuQueryRepository
    {
        public Task<List<MenuItem>> GetActiveMenuItemsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(items.Where(item => item.IsActive).ToList());
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
