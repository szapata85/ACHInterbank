using System.Security.Claims;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.DataBase.Queries.Navigation;
using Cfa.ACHInterbank.Application.Navigation;
using Cfa.ACHInterbank.Application.Navigation.Queries;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav34NConnectionsNavigationTests
{
    private static readonly string[] LegacyIntegrationChildRoutes =
    [
        "/integraciones/contrato",
        "/integraciones/relacion-informacion",
        "/integraciones/conexion"
    ];

    [Fact]
    public async Task StructuralSeeder_PreservesCanonicalTree_UsesExistingPermissionUnion_AndRemovesRoleGates()
    {
        await using var fixture = CreateFixture();
        await SeedThroughNav34MAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var beforeGrants = await RoleGrantsAsync(fixture.Context);
        var beforeLegacyChildren = await AuthorizationSnapshotsAsync(fixture.Context, LegacyIntegrationChildRoutes);

        await new ConnectionsStructuralMenuAuthorizationSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var parameters = await fixture.Context.MenuItems.AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.ParametersAdministrationRoute);
        var connections = await fixture.Context.MenuItems.AsNoTracking()
            .SingleAsync(item => item.Route == ConnectionsStructuralMenuAuthorizationSeeder.ConnectionsRoute);

        Assert.Equal(MenuItemConfiguration.IntegrationsId, connections.Id);
        Assert.Equal("Conexiones", connections.Label);
        Assert.Equal("/integraciones", connections.Route);
        Assert.Equal(parameters.Id, connections.ParentId);
        Assert.Equal(80, connections.Order);
        Assert.True(connections.IsActive);

        var children = await fixture.Context.MenuItems.AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == connections.Id)
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order })
            .ToArrayAsync();

        Assert.Equal(
            [
                ("/integraciones/contrato", "Contrato del servicio", 10),
                ("/integraciones/relacion-informacion", "Relación de información", 20),
                ("/integraciones/conexion", "Conexión entidad participante", 30),
                ("/administracion/mft-ach", "Configuración del intercambio de archivos", 40)
            ],
            children.Select(item => (item.Route, item.Label, item.Order)).ToArray());

        var parentPermissions = await PermissionNamesAsync(fixture.Context, connections.Id);
        Assert.Equal(
            ["CanManageUsers", "CanReadAch", FineGrainedPermissions.Config.Read],
            parentPermissions.OrderBy(value => value, StringComparer.Ordinal).ToArray());
        Assert.Empty(await RoleNamesAsync(fixture.Context, connections.Id));

        var mftId = await fixture.Context.MenuItems.AsNoTracking()
            .Where(item => item.Route == AchColombiaMftAdministrationMenuSeeder.Route)
            .Select(item => item.Id)
            .SingleAsync();
        Assert.Equal(
            ["CanReadAch", FineGrainedPermissions.Config.Read],
            (await PermissionNamesAsync(fixture.Context, mftId)).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        Assert.Empty(await RoleNamesAsync(fixture.Context, mftId));

        Assert.Equal(beforeLegacyChildren, await AuthorizationSnapshotsAsync(fixture.Context, LegacyIntegrationChildRoutes));
        Assert.Equal(beforeGrants, await RoleGrantsAsync(fixture.Context));

        await new ConnectionsStructuralMenuAuthorizationSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        Assert.Equal(beforeLegacyChildren, await AuthorizationSnapshotsAsync(fixture.Context, LegacyIntegrationChildRoutes));
        Assert.Equal(beforeGrants, await RoleGrantsAsync(fixture.Context));
        Assert.Empty(await RoleNamesAsync(fixture.Context, connections.Id));
        Assert.Empty(await RoleNamesAsync(fixture.Context, mftId));
    }

    [Fact]
    public async Task MenuFiltering_ConfigReadOnly_KeepsParametersAndConnectionsAroundOnlyMftChild()
    {
        await using var fixture = CreateFixture();
        var repository = await SeedAndBuildRepositoryAsync(fixture.Context);

        var menu = await MenuFor(repository, [new Claim("permission", FineGrainedPermissions.Config.Read)]);

        AssertSingleMftBranch(menu);
    }

    [Fact]
    public async Task MenuFiltering_CanReadAchOnly_KeepsParametersAndConnectionsAroundOnlyMftChild()
    {
        await using var fixture = CreateFixture();
        var repository = await SeedAndBuildRepositoryAsync(fixture.Context);

        var menu = await MenuFor(repository, [new Claim("permission", "CanReadAch")]);

        AssertSingleMftBranch(menu);
    }

    [Fact]
    public async Task MenuFiltering_CurrentLegacyChildAuthorization_KeepsParentWithoutGrantingMft()
    {
        await using var fixture = CreateFixture();
        var repository = await SeedAndBuildRepositoryAsync(fixture.Context);

        var menu = await MenuFor(repository,
        [
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("permission", "CanManageUsers")
        ]);

        var parameters = Assert.Single(menu);
        Assert.Equal(NavigationInformationArchitectureSeeder.ParametersAdministrationRoute, parameters.Route);
        var connections = Assert.Single(parameters.Children);
        Assert.Equal("/integraciones", connections.Route);
        Assert.Equal(LegacyIntegrationChildRoutes, connections.Children.Select(item => item.Route).ToArray());
        Assert.DoesNotContain(connections.Children, item => item.Route == AchColombiaMftAdministrationMenuSeeder.Route);
    }

    [Fact]
    public async Task MenuFiltering_NoChildCapability_DoesNotExposeConnectionsOrItsChildren()
    {
        await using var fixture = CreateFixture();
        var repository = await SeedAndBuildRepositoryAsync(fixture.Context);

        var menu = await MenuFor(repository, [new Claim("permission", "CanManageAch")]);
        var routes = Flatten(menu).Select(item => item.Route).ToArray();

        Assert.DoesNotContain("/integraciones", routes);
        Assert.DoesNotContain("/integraciones/contrato", routes);
        Assert.DoesNotContain("/integraciones/relacion-informacion", routes);
        Assert.DoesNotContain("/integraciones/conexion", routes);
        Assert.DoesNotContain("/administracion/mft-ach", routes);
    }

    [Fact]
    public async Task SourceRegression_HandlerAndMftSpaApiRemainGenericAndNav34MPoliciesStayUnchanged()
    {
        var root = RepositoryRoot();
        var handler = await Read(root, "src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs");
        var routing = await Read(root, "web/ach-interbank-ui/src/app/app-routing.module.ts");
        var mftComponent = await Read(root, "web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.ts");
        var fileController = await Read(root, "src/Cfa.ACHInterbank.Api/Controllers/AchColombiaFileExchangeController.cs");
        var scheduleController = await Read(root, "src/Cfa.ACHInterbank.Api/Controllers/AchColombiaCycleOperationalSchedulesController.cs");

        Assert.DoesNotContain("Config.Read", handler);
        Assert.DoesNotContain("/integraciones", handler);
        Assert.Contains("permissions: ['Config.Read', 'CanReadAch']", Slice(routing, "path: 'administracion/mft-ach'", "path: 'ach-colombia/file-exchange'"));
        Assert.Contains("this.auth.hasPermission(['Config.Manage', 'CanManageAch'])", mftComponent);
        Assert.Contains("[Authorize(Policy = P1Policies.ConfigRead)]", fileController);
        Assert.Contains("[Authorize(Policy = P1Policies.ConfigManage)]", fileController);
        Assert.Contains("[Authorize(Policy = P1Policies.ConfigRead)]", scheduleController);
        Assert.Contains("[Authorize(Policy = P1Policies.ConfigManage)]", scheduleController);
    }

    private static void AssertSingleMftBranch(IList<MenuItemDto> menu)
    {
        var parameters = Assert.Single(menu);
        Assert.Equal(NavigationInformationArchitectureSeeder.ParametersAdministrationRoute, parameters.Route);
        var connections = Assert.Single(parameters.Children);
        Assert.Equal("Conexiones", connections.Label);
        Assert.Equal("/integraciones", connections.Route);
        var child = Assert.Single(connections.Children);
        Assert.Equal("Configuración del intercambio de archivos", child.Label);
        Assert.Equal("/administracion/mft-ach", child.Route);
        Assert.DoesNotContain(menu, item => item.Route == "/administracion/mft-ach");
    }

    private static async Task<StaticMenuRepository> SeedAndBuildRepositoryAsync(AchDbContext context)
    {
        await SeedThroughNav34MAsync(context);
        await new ConnectionsStructuralMenuAuthorizationSeeder(context).SeedAsync();
        context.ChangeTracker.Clear();

        var relevantRoutes = new HashSet<string>(StringComparer.Ordinal)
        {
            NavigationInformationArchitectureSeeder.ParametersAdministrationRoute,
            "/integraciones",
            "/integraciones/contrato",
            "/integraciones/relacion-informacion",
            "/integraciones/conexion",
            "/administracion/mft-ach"
        };

        var items = await context.MenuItems.AsNoTracking()
            .Include(item => item.MenuItemRoles).ThenInclude(link => link.Role)
            .Include(item => item.MenuItemPermissions).ThenInclude(link => link.Permission)
            .Where(item => relevantRoutes.Contains(item.Route))
            .ToListAsync();

        return new StaticMenuRepository(items);
    }

    private static async Task SeedThroughNav34MAsync(AchDbContext context)
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
        {
            await seeder.SeedAsync();
        }

        await new NavigationInformationArchitectureSeeder(context).SeedAsync();
        await new MftAdministrationMenuAuthorizationSeeder(context).SeedAsync();
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

    private static async Task<string[]> AuthorizationSnapshotsAsync(AchDbContext context, IEnumerable<string> routes)
    {
        var snapshots = new List<string>();
        foreach (var route in routes)
        {
            var id = await context.MenuItems.AsNoTracking().Where(item => item.Route == route).Select(item => item.Id).SingleAsync();
            var permissions = await PermissionNamesAsync(context, id);
            var roles = await RoleNamesAsync(context, id);
            snapshots.Add($"{route}|R={string.Join(',', roles)}|P={string.Join(',', permissions)}");
        }
        return snapshots.OrderBy(value => value, StringComparer.Ordinal).ToArray();
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
