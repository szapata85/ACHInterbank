using System.Reflection;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Navigation.Dtos;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
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

public sealed class NavigationMenuNav34HMenuOrganizationAuthorizationTests
{
    [Fact]
    public async Task DedicatedPermission_IsPersistedIdempotently_WithoutRoleGrant()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        var grantsBefore = await RoleGrants(context);

        var seeder = new NavigationMenuPermissionSeeder(context);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        Assert.Contains(FineGrainedPermissions.NavigationMenu.Manage, FineGrainedPermissions.AllPermissions);
        var permission = await context.Permissions.AsNoTracking()
            .SingleAsync(item => item.Name == FineGrainedPermissions.NavigationMenu.Manage);
        Assert.Equal(NavigationMenuPermissionSeeder.PermissionId, permission.Id);
        Assert.Equal("Administrar organización del menú", permission.Description);
        Assert.Empty(await context.RolePermissions.AsNoTracking()
            .Where(link => link.PermissionId == permission.Id)
            .ToArrayAsync());
        Assert.Equal(grantsBefore, await RoleGrants(context));
    }

    [Fact]
    public async Task LegacyAudience_IsAdminOnlyInCurrentSeed_AndOperatorDoesNotReceiveManageUsers()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;

        var manageUsersGrants = await context.RolePermissions.AsNoTracking()
            .Where(link => link.PermissionId == PermissionConfiguration.ManageUsersPermissionId)
            .OrderBy(link => link.RoleId)
            .Select(link => link.RoleId)
            .ToArrayAsync();

        Assert.Equal(new[] { RoleConfiguration.AdminRoleId }, manageUsersGrants);
        Assert.DoesNotContain(RoleConfiguration.OperatorRoleId, manageUsersGrants);
        Assert.Equal(2, await context.Roles.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task MenuAuthorization_UsesSemanticOrLegacyAlternatives_WithoutRoleGate()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;

        await new NavigationMenuPermissionSeeder(context).SeedAsync();
        var grantsBefore = await RoleGrants(context);

        var seeder = new NavigationMenuAuthorizationSeeder(context);
        Assert.True(NavigationInformationArchitectureSeeder.FinalReconciliationOrder < seeder.Order);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var item = await context.MenuItems.AsNoTracking()
            .SingleAsync(value => value.Id == MenuItemConfiguration.NavigationAdminId);
        Assert.Equal(NavigationInformationArchitectureSeeder.MenuOrganizationRoute, item.Route);

        var permissions = await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == item.Id)
            .OrderBy(link => link.PermissionId)
            .Select(link => link.PermissionId)
            .ToArrayAsync();
        Assert.Equal(
            new[] { NavigationMenuPermissionSeeder.PermissionId, PermissionConfiguration.ManageUsersPermissionId }
                .OrderBy(id => id).ToArray(),
            permissions);
        Assert.Empty(await context.MenuItemRoles.AsNoTracking().Where(link => link.MenuItemId == item.Id).ToArrayAsync());
        Assert.Equal(grantsBefore, await RoleGrants(context));
    }

    [Fact]
    public async Task Policy_AcceptsDedicatedOrValidatedLegacyFallback_ButNotUnrelatedClaims()
    {
        using var provider = Provider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await authorization.AuthorizeAsync(User(FineGrainedPermissions.NavigationMenu.Manage), null, P1Policies.NavigationMenuManage)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(User("CanManageUsers"), null, P1Policies.NavigationMenuManage)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(User("CanReadAch"), null, P1Policies.NavigationMenuManage)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(User("CanManageAch"), null, P1Policies.NavigationMenuManage)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, P1Policies.NavigationMenuManage)).Succeeded);
    }

    [Fact]
    public void Controller_AllCrudEndpointsShareSemanticPolicy_AndNoRoleNameBoundary()
    {
        var controllerType = typeof(MenuItemsController);
        var authorize = Assert.Single(controllerType.GetCustomAttributes<AuthorizeAttribute>(true));
        Assert.Equal(P1Policies.NavigationMenuManage, authorize.Policy);
        Assert.True(string.IsNullOrWhiteSpace(authorize.Roles));

        AssertAction<MenuItemsController>(nameof(MenuItemsController.GetMenuItemsAsync), typeof(HttpGetAttribute));
        AssertAction<MenuItemsController>(nameof(MenuItemsController.CreateMenuItemAsync), typeof(HttpPostAttribute));
        AssertAction<MenuItemsController>(nameof(MenuItemsController.UpdateMenuItemAsync), typeof(HttpPutAttribute));
        AssertAction<MenuItemsController>(nameof(MenuItemsController.DeleteMenuItemAsync), typeof(HttpDeleteAttribute));
    }

    [Fact]
    public async Task SpaAndService_RequireSemanticCapability_AndExposeStructurePlusAuthorizationMutation()
    {
        var root = RepositoryRoot();
        var appRouting = await Read(root, "web/ach-interbank-ui/src/app/app-routing.module.ts");
        var navigationRouting = await Read(root, "web/ach-interbank-ui/src/app/features/navigation/navigation-routing.module.ts");
        var component = await Read(root, "web/ach-interbank-ui/src/app/features/navigation/components/navigation-menu.component.ts");
        var service = await Read(root, "web/ach-interbank-ui/src/app/features/navigation/services/navigation-menu.service.ts");
        var persistence = await Read(root, "src/Cfa.ACHInterbank.Persistence/Navigation/Services/MenuItemsService.cs");

        var navigationParent = Slice(appRouting, "path: 'navigation'", "path: 'audit-logs'");
        Assert.Contains("canActivate: [permissionGuard]", navigationParent);
        Assert.Contains("'NavigationMenu.Manage'", navigationParent);
        Assert.Contains("'CanManageUsers'", navigationParent);
        Assert.DoesNotContain("roles:", navigationParent);

        Assert.Contains("redirectTo: 'menu-items'", navigationRouting);
        Assert.Contains("path: 'menu-items'", navigationRouting);
        Assert.Contains("canActivate: [permissionGuard]", navigationRouting);
        Assert.Contains("permissions: ['NavigationMenu.Manage', 'CanManageUsers']", navigationRouting);

        Assert.Contains("hasPermission(['NavigationMenu.Manage', 'CanManageUsers'])", component);
        Assert.DoesNotContain("hasRole('Admin')", component);
        Assert.Contains("roleIds: [[] as string[]]", component);
        Assert.Contains("permissionIds: [[] as string[]]", component);
        Assert.Contains("payload.isActive = !item.isActive", component);

        Assert.Contains("getMenuItems()", service);
        Assert.Contains("createMenuItem", service);
        Assert.Contains("updateMenuItem", service);
        Assert.Contains("deleteMenuItem", service);
        Assert.Contains("api/navigation/menu-items", service);

        Assert.Contains("menuItem.ParentId = request.ParentId", persistence);
        Assert.Contains("menuItem.Label = request.Label", persistence);
        Assert.Contains("menuItem.Route = request.Route", persistence);
        Assert.Contains("menuItem.Order = request.Order", persistence);
        Assert.Contains("menuItem.Exact = request.Exact", persistence);
        Assert.Contains("menuItem.IsActive = request.IsActive", persistence);
        Assert.Contains("UpdateRelationsAsync(menuItem, request.RoleIds, request.PermissionIds", persistence);
        Assert.Contains("_dbContext.MenuItems.Remove(menuItem)", persistence);
    }

    [Fact]
    public async Task SupportingRoleAndPermissionCatalogReads_AcceptNavigationMenuManage()
    {
        using var provider = Provider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var principal = User(FineGrainedPermissions.NavigationMenu.Manage);

        Assert.True((await authorization.AuthorizeAsync(principal, null, P1Policies.RolesRead)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(principal, null, P1Policies.PermissionsRead)).Succeeded);
    }

    [Fact]
    public async Task Regression_SystemRecordsReportsAndHandlerRemainUntouched()
    {
        var root = RepositoryRoot();
        var appRouting = await Read(root, "web/ach-interbank-ui/src/app/app-routing.module.ts");
        var handler = await Read(root, "src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs");
        var reportsRouting = await Read(root, "web/ach-interbank-ui/src/app/features/reports/reports-routing.module.ts");

        Assert.Contains("permissions: ['NavigationLogs.Read', 'CanReadAch']", appRouting);
        Assert.Contains("path: 'audit'", reportsRouting);
        Assert.Contains("permissions: ['CanReadAch']", reportsRouting);
        Assert.Contains("return hasRole && hasPermission;", handler);
    }

    private static void AssertAction<TController>(string name, Type httpAttributeType)
    {
        var method = typeof(TController).GetMethod(name);
        Assert.NotNull(method);
        Assert.Contains(method!.GetCustomAttributes(true), attribute => httpAttributeType.IsInstanceOfType(attribute));
    }

    private static async Task<string[]> RoleGrants(AchDbContext context)
        => (await context.RolePermissions.AsNoTracking()
            .OrderBy(link => link.RoleId)
            .ThenBy(link => link.PermissionId)
            .Select(link => new { link.RoleId, link.PermissionId })
            .ToArrayAsync())
            .Select(link => $"{link.RoleId}|{link.PermissionId}")
            .ToArray();

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

    private static ServiceProvider Provider()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["appSettings:tokenManager:issuerJwt"] = "issuer",
                ["appSettings:tokenManager:audienceJwt"] = "audience",
                ["appSettings:tokenManager:secretKetJwt"] = "this-is-a-test-secret-key-with-32-bytes"
            })
            .Build();
        services.AddExternal(configuration);
        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal User(string permission)
        => new(new ClaimsIdentity([new Claim("permission", permission)], "test"));

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
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory!.FullName;
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public Fixture(AchDbContext context, SqliteConnection connection)
        {
            Context = context;
            Connection = connection;
        }

        public AchDbContext Context { get; }
        private SqliteConnection Connection { get; }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
