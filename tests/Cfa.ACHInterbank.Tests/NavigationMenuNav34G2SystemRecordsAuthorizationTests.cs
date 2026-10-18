using System.Reflection;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.DataBase.Queries.Navigation;
using Cfa.ACHInterbank.Application.Navigation;
using Cfa.ACHInterbank.Application.Navigation.Queries;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav34G2SystemRecordsAuthorizationTests
{
    [Fact]
    public async Task DedicatedPermissions_ArePersistedIdempotently_WithoutRoleGrants()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        var grantsBefore = await RoleGrants(context);

        var seeder = new SystemRecordPermissionsSeeder(context);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var expected = new Dictionary<string, (Guid Id, string Description)>
        {
            [FineGrainedPermissions.AuditLogs.Read] = (SystemRecordPermissionsSeeder.AuditReadPermissionId, "Consultar registro de auditoría"),
            [FineGrainedPermissions.AuthLogs.Read] = (SystemRecordPermissionsSeeder.AuthReadPermissionId, "Consultar registro de accesos"),
            [FineGrainedPermissions.NavigationLogs.Read] = (SystemRecordPermissionsSeeder.NavigationReadPermissionId, "Consultar registro de navegación")
        };

        foreach (var item in expected)
        {
            Assert.Contains(item.Key, FineGrainedPermissions.AllPermissions);
            var permission = await context.Permissions.AsNoTracking().SingleAsync(value => value.Name == item.Key);
            Assert.Equal(item.Value.Id, permission.Id);
            Assert.Equal(item.Value.Description, permission.Description);
            Assert.Empty(await context.RolePermissions.AsNoTracking().Where(link => link.PermissionId == permission.Id).ToArrayAsync());
        }

        Assert.Equal(grantsBefore, await RoleGrants(context));
    }

    [Fact]
    public async Task MenuAuthorization_UsesDedicatedOrLegacyAlternatives_AndParentUsesUnion()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        var navigation = await AddNavigationMenuItem(context);

        await new SystemRecordPermissionsSeeder(context).SeedAsync();
        var grantsBefore = await RoleGrants(context);
        var roleLinksBefore = await RoleLinks(context, [MenuItemConfiguration.LogsId, MenuItemConfiguration.AuditLogId, MenuItemConfiguration.AuthLogId, navigation.Id]);

        var seeder = new SystemRecordsMenuAuthorizationSeeder(context);
        Assert.True(NavigationInformationArchitectureSeeder.FinalReconciliationOrder < seeder.Order);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        Assert.Equal(
            new[] { SystemRecordPermissionsSeeder.AuditReadPermissionId, PermissionConfiguration.ManageUsersPermissionId }.OrderBy(value => value).ToArray(),
            await PermissionIds(context, MenuItemConfiguration.AuditLogId));
        Assert.Equal(
            new[] { SystemRecordPermissionsSeeder.AuthReadPermissionId, PermissionConfiguration.ManageUsersPermissionId }.OrderBy(value => value).ToArray(),
            await PermissionIds(context, MenuItemConfiguration.AuthLogId));
        Assert.Equal(
            new[] { SystemRecordPermissionsSeeder.NavigationReadPermissionId, PermissionConfiguration.ReadAchPermissionId }.OrderBy(value => value).ToArray(),
            await PermissionIds(context, navigation.Id));
        Assert.Equal(
            new[]
            {
                SystemRecordPermissionsSeeder.AuditReadPermissionId,
                SystemRecordPermissionsSeeder.AuthReadPermissionId,
                SystemRecordPermissionsSeeder.NavigationReadPermissionId,
                PermissionConfiguration.ManageUsersPermissionId,
                PermissionConfiguration.ReadAchPermissionId
            }.OrderBy(value => value).ToArray(),
            await PermissionIds(context, MenuItemConfiguration.LogsId));

        Assert.Equal(roleLinksBefore, await RoleLinks(context, [MenuItemConfiguration.LogsId, MenuItemConfiguration.AuditLogId, MenuItemConfiguration.AuthLogId, navigation.Id]));
        Assert.Equal(grantsBefore, await RoleGrants(context));
    }

    [Fact]
    public async Task MenuHandler_KeepsAuthorizedChildrenNested_WithoutSiblingLeakage()
    {
        var administration = new MenuItem
        {
            Id = 6000,
            Label = NavigationInformationArchitectureSeeder.AdministrationLabel,
            Route = NavigationInformationArchitectureSeeder.AdministrationRoute,
            Order = 80,
            Exact = false,
            IsActive = true
        };
        var records = new MenuItem
        {
            Id = MenuItemConfiguration.LogsId,
            ParentId = administration.Id,
            Label = NavigationInformationArchitectureSeeder.SystemRecordsLabel,
            Route = NavigationInformationArchitectureSeeder.SystemRecordsRoute,
            Order = 30,
            Exact = false,
            IsActive = true
        };
        AddPermissions(records,
            FineGrainedPermissions.AuditLogs.Read,
            FineGrainedPermissions.AuthLogs.Read,
            FineGrainedPermissions.NavigationLogs.Read,
            "CanManageUsers",
            "CanReadAch");

        var audit = Child(MenuItemConfiguration.AuditLogId, records.Id, NavigationInformationArchitectureSeeder.AuditLogLabel,
            NavigationInformationArchitectureSeeder.AuditLogRoute, 10, FineGrainedPermissions.AuditLogs.Read, "CanManageUsers");
        var access = Child(MenuItemConfiguration.AuthLogId, records.Id, NavigationInformationArchitectureSeeder.AuthenticationLogLabel,
            NavigationInformationArchitectureSeeder.AuthenticationLogRoute, 20, FineGrainedPermissions.AuthLogs.Read, "CanManageUsers");
        var navigation = Child(6001, records.Id, NavigationInformationArchitectureSeeder.NavigationLogLabel,
            NavigationInformationArchitectureSeeder.NavigationLogRoute, 30, FineGrainedPermissions.NavigationLogs.Read, "CanReadAch");
        var repository = new StaticMenuRepository([administration, records, audit, access, navigation]);

        await AssertVisibleChildren(repository, FineGrainedPermissions.NavigationLogs.Read,
            NavigationInformationArchitectureSeeder.NavigationLogRoute);
        await AssertVisibleChildren(repository, "CanReadAch",
            NavigationInformationArchitectureSeeder.NavigationLogRoute);
        await AssertVisibleChildren(repository, FineGrainedPermissions.AuditLogs.Read,
            NavigationInformationArchitectureSeeder.AuditLogRoute);
        await AssertVisibleChildren(repository, FineGrainedPermissions.AuthLogs.Read,
            NavigationInformationArchitectureSeeder.AuthenticationLogRoute);
        await AssertVisibleChildren(repository, "CanManageUsers",
            NavigationInformationArchitectureSeeder.AuditLogRoute,
            NavigationInformationArchitectureSeeder.AuthenticationLogRoute);
    }

    [Fact]
    public void Controllers_RequireDedicatedReadPolicies_WhileNavigationTelemetryPostRemainsAuthenticatedOnly()
    {
        AssertGetPolicy<AuditLogsController>(nameof(AuditLogsController.GetAuditLogsAsync), P1Policies.AuditLogsRead);
        AssertGetPolicy<AuthLogsController>(nameof(AuthLogsController.GetAuthLogsAsync), P1Policies.AuthLogsRead);
        AssertGetPolicy<NavigationLogsController>(nameof(NavigationLogsController.GetNavigationLogsAsync), P1Policies.NavigationLogsRead);

        var post = typeof(NavigationLogsController).GetMethod(nameof(NavigationLogsController.AddNavigationLogAsync));
        Assert.NotNull(post);
        Assert.NotNull(post!.GetCustomAttribute<HttpPostAttribute>());
        Assert.Empty(post.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Contains(typeof(NavigationLogsController).GetCustomAttributes<AuthorizeAttribute>(), attribute => attribute.Policy is null);
    }

    [Fact]
    public async Task PoliciesAndSpaRoutes_UseOrFallbacks_AndReportsAuditRemainsSeparate()
    {
        var root = RepositoryRoot();
        var policyRegistration = await Read(root, "src/Cfa.ACHInterbank.External/DependencyInjectionService.cs");
        var appRouting = await Read(root, "web/ach-interbank-ui/src/app/app-routing.module.ts");
        var reportsRouting = await Read(root, "web/ach-interbank-ui/src/app/features/reports/reports-routing.module.ts");
        var reportsApi = await Read(root, "web/ach-interbank-ui/src/app/core/services/reports-api.service.ts");
        var auditApi = await Read(root, "web/ach-interbank-ui/src/app/features/audit/services/audit-log.service.ts");
        var handler = await Read(root, "src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs");
        var telemetry = await Read(root, "web/ach-interbank-ui/src/app/core/services/navigation-log.service.ts");

        var auditPolicy = Slice(policyRegistration, "options.AddPolicy(P1Policies.AuditLogsRead", "options.AddPolicy(P1Policies.AuthLogsRead");
        Assert.Contains("FineGrainedPermissions.AuditLogs.Read", auditPolicy);
        Assert.Contains("\"CanManageUsers\"", auditPolicy);
        var authPolicy = Slice(policyRegistration, "options.AddPolicy(P1Policies.AuthLogsRead", "options.AddPolicy(P1Policies.NavigationLogsRead");
        Assert.Contains("FineGrainedPermissions.AuthLogs.Read", authPolicy);
        Assert.Contains("\"CanManageUsers\"", authPolicy);
        var navigationPolicy = Slice(policyRegistration, "options.AddPolicy(P1Policies.NavigationLogsRead", "options.AddPolicy(P1Policies.SchedulerView");
        Assert.Contains("FineGrainedPermissions.NavigationLogs.Read", navigationPolicy);
        Assert.Contains("\"CanReadAch\"", navigationPolicy);

        AssertRoutePermissions(appRouting, "audit-logs", "auth-logs", "AuditLogs.Read", "CanManageUsers");
        AssertRoutePermissions(appRouting, "auth-logs", "navigation-logs", "AuthLogs.Read", "CanManageUsers");
        AssertRoutePermissions(appRouting, "navigation-logs", "customer-third-parties", "NavigationLogs.Read", "CanReadAch");

        Assert.Contains("path: 'audit'", reportsRouting);
        Assert.Contains("permissions: ['CanReadAch']", reportsRouting);
        Assert.Contains("api/reports/audit", reportsApi);
        Assert.Contains("api/audit-logs", auditApi);

        Assert.Contains("if (item.ParentId.HasValue && dtoLookup.TryGetValue(item.ParentId.Value, out var parentDto))", handler);
        Assert.Contains("roots.Add(dto);", handler);
        Assert.Contains(".post('api/navigation-logs'", telemetry);
        Assert.Contains("if (!this.auth.isAuthenticated())", telemetry);
        Assert.DoesNotContain("NavigationLogs.Read", telemetry);
        Assert.DoesNotContain("CanReadAch", telemetry);
    }

    private static void AssertGetPolicy<TController>(string methodName, string expectedPolicy)
    {
        var get = typeof(TController).GetMethod(methodName);
        Assert.NotNull(get);
        Assert.NotNull(get!.GetCustomAttribute<HttpGetAttribute>());
        Assert.Contains(get.GetCustomAttributes<AuthorizeAttribute>(), attribute => attribute.Policy == expectedPolicy);
        Assert.Contains(typeof(TController).GetCustomAttributes<AuthorizeAttribute>(), attribute => attribute.Policy is null);
    }

    private static async Task AssertVisibleChildren(StaticMenuRepository repository, string permission, params string[] expectedRoutes)
    {
        var menu = await MenuFor(repository, permission);
        var administration = Assert.Single(menu.Where(item => item.Route == NavigationInformationArchitectureSeeder.AdministrationRoute));
        var records = Assert.Single(administration.Children.Where(item => item.Route == NavigationInformationArchitectureSeeder.SystemRecordsRoute));
        Assert.Equal(expectedRoutes, records.Children.OrderBy(item => item.Order).Select(item => item.Route).ToArray());

        foreach (var route in expectedRoutes)
            Assert.DoesNotContain(menu, item => item.Route == route);
    }

    private static void AssertRoutePermissions(string appRouting, string startPath, string endPath, params string[] permissions)
    {
        var route = Slice(appRouting, $"path: '{startPath}'", $"path: '{endPath}'");
        Assert.Contains("canActivate: [permissionGuard]", route);
        foreach (var permission in permissions)
            Assert.Contains($"'{permission}'", route);
        Assert.DoesNotContain("roles:", route);
    }

    private static async Task<MenuItem> AddNavigationMenuItem(AchDbContext context)
    {
        var existing = await context.MenuItems.SingleOrDefaultAsync(item => item.Route == NavigationInformationArchitectureSeeder.NavigationLogRoute);
        if (existing is not null)
            return existing;

        var item = new MenuItem
        {
            Id = 5034,
            MenuId = MenuConfiguration.MainMenuId,
            ParentId = MenuItemConfiguration.LogsId,
            Label = NavigationInformationArchitectureSeeder.NavigationLogLabel,
            Route = NavigationInformationArchitectureSeeder.NavigationLogRoute,
            Icon = "route",
            Order = 30,
            Exact = true,
            IsActive = true
        };
        context.MenuItems.Add(item);
        context.MenuItemPermissions.Add(new MenuItemPermission
        {
            MenuItemId = item.Id,
            PermissionId = PermissionConfiguration.ReadAchPermissionId
        });
        await context.SaveChangesAsync();
        return item;
    }

    private static MenuItem Child(int id, int parentId, string label, string route, int order, params string[] permissions)
    {
        var item = new MenuItem
        {
            Id = id,
            ParentId = parentId,
            Label = label,
            Route = route,
            Order = order,
            Exact = true,
            IsActive = true
        };
        AddPermissions(item, permissions);
        return item;
    }

    private static void AddPermissions(MenuItem item, params string[] permissions)
    {
        foreach (var permission in permissions)
        {
            item.MenuItemPermissions.Add(new MenuItemPermission
            {
                MenuItemId = item.Id,
                Permission = new Permission { Name = permission }
            });
        }
    }

    private static async Task<IList<MenuItemDto>> MenuFor(StaticMenuRepository repository, params string[] permissions)
        => await new GetMenuForCurrentUserHandler(
                repository,
                new TestCurrentUserContext(permissions.Select(permission => new Claim("permission", permission))))
            .Handle(new GetMenuForCurrentUserQuery(), CancellationToken.None);

    private static async Task<Guid[]> PermissionIds(AchDbContext context, int menuItemId)
        => await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == menuItemId)
            .OrderBy(link => link.PermissionId)
            .Select(link => link.PermissionId)
            .ToArrayAsync();

    private static async Task<string[]> RoleLinks(AchDbContext context, int[] menuItemIds)
        => (await context.MenuItemRoles.AsNoTracking()
            .Where(link => menuItemIds.Contains(link.MenuItemId))
            .OrderBy(link => link.MenuItemId)
            .ThenBy(link => link.RoleId)
            .Select(link => new { link.MenuItemId, link.RoleId })
            .ToArrayAsync())
            .Select(link => $"{link.MenuItemId}|{link.RoleId}")
            .ToArray();

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
