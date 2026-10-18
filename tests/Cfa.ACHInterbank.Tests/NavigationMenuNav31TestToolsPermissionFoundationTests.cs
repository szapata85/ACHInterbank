using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav31TestToolsPermissionFoundationTests
{
    [Fact]
    public async Task PermissionSeeder_CreatesDedicatedAccessPermissionIdempotently_WithoutGrantingCurrentFunctionalRoles()
    {
        await using var fixture = CreateFixture();
        var roleSnapshotBefore = await fixture.Context.Roles
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new { item.Id, item.Name, item.Description })
            .ToArrayAsync();
        var rolePermissionCountBefore = await fixture.Context.RolePermissions.CountAsync();

        var sut = new TestToolsPermissionSeeder(fixture.Context);
        await sut.SeedAsync();
        await sut.SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var permission = await fixture.Context.Permissions
            .AsNoTracking()
            .SingleAsync(item => item.Name == FineGrainedPermissions.TestTools.Access);

        Assert.Equal(TestToolsPermissionSeeder.AccessPermissionId, permission.Id);
        Assert.Equal("Acceso a herramientas de prueba", permission.Description);
        Assert.Contains(FineGrainedPermissions.TestTools.Access, FineGrainedPermissions.AllPermissions);

        Assert.Empty(await fixture.Context.RolePermissions
            .AsNoTracking()
            .Where(item => item.PermissionId == permission.Id)
            .ToArrayAsync());
        Assert.Equal(rolePermissionCountBefore, await fixture.Context.RolePermissions.CountAsync());

        var roleSnapshotAfter = await fixture.Context.Roles
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new { item.Id, item.Name, item.Description })
            .ToArrayAsync();
        Assert.Equal(roleSnapshotBefore, roleSnapshotAfter);
        Assert.Equal(
            new[] { "ACH.Operator", "Admin" },
            roleSnapshotAfter.Select(item => item.Name).ToArray());
        Assert.DoesNotContain(roleSnapshotAfter, item =>
            item.Name!.Contains("Super", StringComparison.OrdinalIgnoreCase)
            || item.Name.Contains("Develop", StringComparison.OrdinalIgnoreCase)
            || item.Name.Contains("Support", StringComparison.OrdinalIgnoreCase)
            || item.Name.Contains("Soporte", StringComparison.OrdinalIgnoreCase)
            || item.Name.Contains("Desarrollo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Nav31Foundation_RemainsIntactWhileLaterPhasesEnforceDedicatedAccessAndSimulatorProductionEligibility()
    {
        var root = RepositoryRoot();
        var navigationSeeder = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "Navigation", "Seeders", "NavigationInformationArchitectureSeeder.cs"));
        var menuPermissionConfiguration = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "Configuration", "MenuItemPermissionConfiguration.cs"));
        var menuAuthorizationSeeder = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "Security", "Seeders", "TestToolsMenuAuthorizationSeeder.cs"));
        var menuRoleConfiguration = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "Configuration", "MenuItemRoleConfiguration.cs"));
        var rolePermissionConfiguration = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "Configuration", "RolePermissionConfiguration.cs"));
        var appRouting = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "app-routing.module.ts"));
        var uatRouting = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "features", "uat", "uat-routing.module.ts"));
        var operationalRouting = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "features", "nacha-operational", "nacha-operational-routing.module.ts"));
        var menuHandler = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Application", "Navigation", "Queries", "GetMenuForCurrentUserHandler.cs"));
        var simulatorController = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Api", "Controllers", "NachaInboundSimulatorController.cs"));
        var serviceConsoleController = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Api", "Controllers", "NachaSoapUatConsoleController.cs"));

        Assert.Contains("Reconcile(testTools, TestToolsLabel, parentId: null, order: 90);", navigationSeeder);
        Assert.Contains("serviceTests.IsActive = false;", navigationSeeder);
        Assert.Contains("AddAuthorization(serviceTests.Id, PermissionConfiguration.ManageAchPermissionId);", navigationSeeder);
        Assert.DoesNotContain(FineGrainedPermissions.TestTools.Access, navigationSeeder);

        Assert.Contains("MenuItemConfiguration.UatSimulatorsId", menuPermissionConfiguration);
        Assert.Contains("MenuItemConfiguration.NachaInboundSimulatorId", menuPermissionConfiguration);
        Assert.Contains("PermissionConfiguration.ManageAchPermissionId", menuPermissionConfiguration);
        Assert.Contains("PermissionConfiguration.ReadAchPermissionId", menuPermissionConfiguration);
        Assert.DoesNotContain(FineGrainedPermissions.TestTools.Access, menuPermissionConfiguration);
        Assert.Contains("RoleConfiguration.AdminRoleId", menuRoleConfiguration);
        Assert.Contains("RoleConfiguration.OperatorRoleId", menuRoleConfiguration);
        Assert.DoesNotContain(FineGrainedPermissions.TestTools.Access, rolePermissionConfiguration);

        Assert.Contains(FineGrainedPermissions.TestTools.Access, menuAuthorizationSeeder);
        Assert.Contains("context.MenuItemRoles.RemoveRange(roleLinks);", menuAuthorizationSeeder);

        Assert.Contains("path: 'uat'", appRouting);
        Assert.Contains("permissions: ['TestTools.Access']", Slice(appRouting, "path: 'uat'", "path: 'incoming-nacha-command-center'"));
        Assert.Contains("permissions: ['TestTools.Access']", uatRouting);
        Assert.DoesNotContain("nacha/soap-uat-console", operationalRouting);
        Assert.Contains("permissions: ['TestTools.Access']", Slice(appRouting, "path: 'ach/nacha/soap-uat-console'", "path: 'ach'"));

        Assert.Contains("[Authorize(Policy = FineGrainedPermissions.TestTools.Access)]", simulatorController);
        Assert.Contains("[Authorize(Policy = P1Policies.NachaSimulatorRead)]", simulatorController);
        Assert.Contains("P1Policies.NachaSimulatorGenerateIncoming", simulatorController);
        Assert.Contains("[Authorize(Policy = FineGrainedPermissions.TestTools.Access)]", serviceConsoleController);
        Assert.DoesNotContain("NachaSimulatorRead", serviceConsoleController);

        Assert.DoesNotContain("IsProduction", menuHandler);
        Assert.DoesNotContain("IsUatLike", menuHandler);
        Assert.DoesNotContain("IsProduction", simulatorController);
        Assert.DoesNotContain("IsUatLike", simulatorController);
        Assert.DoesNotContain("IsProduction", serviceConsoleController);
        Assert.DoesNotContain("IsUatLike", serviceConsoleController);
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
