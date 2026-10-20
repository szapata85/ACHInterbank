using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav32ATestToolsAdminRoleGrantTests
{
    [Fact]
    public async Task Seeders_GrantTestToolsAccessOnlyToExistingAdminRole_Idempotently()
    {
        await using var fixture = CreateFixture();

        var rolesBefore = await fixture.Context.Roles
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new { item.Id, item.Name, item.Description })
            .ToArrayAsync();

        Assert.Equal(2, rolesBefore.Length);
        Assert.Contains(rolesBefore, role =>
            role.Id == RoleConfiguration.AdminRoleId
            && role.Name == "Admin"
            && role.Description == "Acceso completo al sistema");
        Assert.Contains(rolesBefore, role =>
            role.Id == RoleConfiguration.OperatorRoleId
            && role.Name == "ACH.Operator");

        var permissionSeeder = new TestToolsPermissionSeeder(fixture.Context);
        var grantSeeder = new TestToolsAdminRolePermissionSeeder(fixture.Context);

        Assert.True(permissionSeeder.Order < grantSeeder.Order);

        await permissionSeeder.SeedAsync();
        await grantSeeder.SeedAsync();
        await permissionSeeder.SeedAsync();
        await grantSeeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var permission = await fixture.Context.Permissions
            .AsNoTracking()
            .SingleAsync(item => item.Name == FineGrainedPermissions.TestTools.Access);

        Assert.Equal(TestToolsPermissionSeeder.AccessPermissionId, permission.Id);

        var grants = await fixture.Context.RolePermissions
            .AsNoTracking()
            .Where(item => item.PermissionId == permission.Id)
            .ToArrayAsync();

        var grant = Assert.Single(grants);
        Assert.Equal(RoleConfiguration.AdminRoleId, grant.RoleId);
        Assert.DoesNotContain(grants, item => item.RoleId == RoleConfiguration.OperatorRoleId);

        var rolesAfter = await fixture.Context.Roles
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new { item.Id, item.Name, item.Description })
            .ToArrayAsync();

        Assert.Equal(rolesBefore, rolesAfter);
        Assert.DoesNotContain(rolesAfter, role =>
            role.Name!.Contains("Super", StringComparison.OrdinalIgnoreCase)
            || role.Name.Contains("Develop", StringComparison.OrdinalIgnoreCase)
            || role.Name.Contains("Support", StringComparison.OrdinalIgnoreCase)
            || role.Name.Contains("Soporte", StringComparison.OrdinalIgnoreCase)
            || role.Name.Contains("Desarrollo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Nav32AGrant_RemainsOnlyAdminWhileLaterPhasesKeepDedicatedPermissionAndRemoveOnlySimulatorEnvironmentGating()
    {
        var root = RepositoryRoot();
        var grantSeeder = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "Security", "Seeders", "TestToolsAdminRolePermissionSeeder.cs"));
        var menuAuthorizationSeeder = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "Security", "Seeders", "TestToolsMenuAuthorizationSeeder.cs"));
        var roleConfiguration = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "Configuration", "RoleConfiguration.cs"));
        var rolePermissionConfiguration = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "Configuration", "RolePermissionConfiguration.cs"));
        var navigationSeeder = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "Navigation", "Seeders", "NavigationInformationArchitectureSeeder.cs"));
        var menuPermissionConfiguration = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "Configuration", "MenuItemPermissionConfiguration.cs"));
        var appRouting = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "app-routing.module.ts"));
        var uatRouting = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "features", "uat", "uat-routing.module.ts"));
        var operationalRouting = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "features", "nacha-operational", "nacha-operational-routing.module.ts"));
        var menuHandler = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Application", "Navigation", "Queries", "GetMenuForCurrentUserHandler.cs"));
        var simulatorController = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Api", "Controllers", "NachaInboundSimulatorController.cs"));
        var serviceConsoleController = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Api", "Controllers", "NachaSoapUatConsoleController.cs"));

        Assert.Contains("RoleConfiguration.AdminRoleId", grantSeeder);
        Assert.Contains("TestToolsPermissionSeeder.AccessPermissionId", grantSeeder);
        Assert.DoesNotContain("RoleConfiguration.OperatorRoleId", grantSeeder);
        Assert.DoesNotContain("UserId", grantSeeder, StringComparison.Ordinal);
        Assert.DoesNotContain("context.Users", grantSeeder, StringComparison.Ordinal);

        Assert.Contains("Id = AdminRoleId", roleConfiguration);
        Assert.Contains("Name = \"Admin\"", roleConfiguration);
        Assert.Contains("Description = \"Acceso completo al sistema\"", roleConfiguration);
        Assert.Contains("Id = OperatorRoleId", roleConfiguration);
        Assert.Contains("Name = \"ACH.Operator\"", roleConfiguration);
        Assert.DoesNotContain("SuperAdmin", roleConfiguration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Developer", roleConfiguration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Support", roleConfiguration, StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(FineGrainedPermissions.TestTools.Access, rolePermissionConfiguration);
        Assert.Contains("serviceTests.IsActive = false;", navigationSeeder);
        Assert.Contains("AddAuthorization(serviceTests.Id, PermissionConfiguration.ManageAchPermissionId);", navigationSeeder);
        Assert.DoesNotContain(FineGrainedPermissions.TestTools.Access, navigationSeeder);
        Assert.DoesNotContain(FineGrainedPermissions.TestTools.Access, menuPermissionConfiguration);
        Assert.Contains(FineGrainedPermissions.TestTools.Access, menuAuthorizationSeeder);
        Assert.Contains("context.MenuItemRoles.RemoveRange(roleLinks);", menuAuthorizationSeeder);

        Assert.Contains("permissions: ['TestTools.Access']", Slice(appRouting, "path: 'uat'", "path: 'incoming-nacha-command-center'"));
        Assert.Contains("permissions: ['TestTools.Access']", uatRouting);
        Assert.DoesNotContain("nacha/soap-uat-console", operationalRouting);
        Assert.Contains("permissions: ['TestTools.Access']", Slice(appRouting, "path: 'ach/nacha/soap-uat-console'", "path: 'ach'"));

        Assert.DoesNotContain("IsProduction", menuHandler);
        Assert.DoesNotContain("IsUatLike", menuHandler);
        Assert.DoesNotContain("IsProduction", simulatorController);
        Assert.DoesNotContain("IsUatLike", simulatorController);
        Assert.DoesNotContain("IsProduction", serviceConsoleController);
        Assert.DoesNotContain("IsUatLike", serviceConsoleController);
        Assert.Contains("[Authorize(Policy = FineGrainedPermissions.TestTools.Access)]", simulatorController);
        Assert.Contains("[Authorize(Policy = P1Policies.NachaSimulatorRead)]", simulatorController);
        Assert.Contains("[Authorize(Policy = FineGrainedPermissions.TestTools.Access)]", serviceConsoleController);
        Assert.DoesNotContain("NachaSimulatorRead", serviceConsoleController);
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
