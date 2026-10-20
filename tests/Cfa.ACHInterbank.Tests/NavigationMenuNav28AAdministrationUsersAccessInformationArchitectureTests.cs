using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav28AAdministrationUsersAccessInformationArchitectureTests
{
    private static readonly (string Route, string Label, int Order)[] PublishedUsersAccessOptions =
    [
        ("/users/list", "Administrar usuarios", 10),
        ("/users/branding", "Identidad visual", 20),
        ("/users/password-rules", "Reglas de contraseña", 30),
        ("/users/login-lockout", "Bloqueo de acceso", 40)
    ];

    private static readonly string[] ExistingUsersAccessRoutes =
    [
        "/users",
        "/users/list",
        "/users/branding",
        "/users/password-rules",
        "/users/login-lockout"
    ];

    [Fact]
    public async Task CanonicalSeeder_BuildsAdministrationUsersAccessFromExistingMenuItems()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var idsBefore = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => ExistingUsersAccessRoutes.Contains(item.Route))
            .OrderBy(item => item.Route)
            .Select(item => new { item.Route, item.Id })
            .ToDictionaryAsync(item => item.Route, item => item.Id);
        Assert.Equal(ExistingUsersAccessRoutes.Length, idsBefore.Count);
        Assert.False(await fixture.Context.MenuItems
            .AsNoTracking()
            .AnyAsync(item => item.Route == NavigationInformationArchitectureSeeder.AdministrationRoute));

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var administration = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.AdministrationRoute);
        Assert.Null(administration.ParentId);
        Assert.Equal(NavigationInformationArchitectureSeeder.AdministrationLabel, administration.Label);
        Assert.Equal(80, administration.Order);

        var usersAccess = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == "/users");
        Assert.Equal(administration.Id, usersAccess.ParentId);
        Assert.Equal(NavigationInformationArchitectureSeeder.UsersAccessLabel, usersAccess.Label);
        Assert.Equal(10, usersAccess.Order);
        Assert.Equal(idsBefore["/users"], usersAccess.Id);

        var children = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == usersAccess.Id)
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order, item.Id })
            .ToListAsync();

        Assert.Equal(
            PublishedUsersAccessOptions,
            children.Select(item => (item.Route, item.Label, item.Order)).ToArray());
        Assert.Equal(children.Count, children.Select(item => item.Order).Distinct().Count());

        foreach (var option in children)
            Assert.Equal(idsBefore[option.Route], option.Id);
    }

    [Fact]
    public async Task CanonicalSeeder_PreservesExistingUsersAccessAuthorizationAndDefinitions()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var ids = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => ExistingUsersAccessRoutes.Contains(item.Route))
            .OrderBy(item => item.Id)
            .Select(item => item.Id)
            .ToArrayAsync();
        Assert.Equal(ExistingUsersAccessRoutes.Length, ids.Length);

        var permissionLinksBefore = await ReadPermissionLinksAsync(fixture.Context, ids);
        var roleLinksBefore = await ReadRoleLinksAsync(fixture.Context, ids);
        var permissionDefinitionsBefore = await ReadPermissionDefinitionsAsync(fixture.Context);
        var roleDefinitionsBefore = await ReadRoleDefinitionsAsync(fixture.Context);
        var roleGrantsBefore = await ReadRoleGrantsAsync(fixture.Context);

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        Assert.Equal(permissionLinksBefore, await ReadPermissionLinksAsync(fixture.Context, ids));
        Assert.Equal(roleLinksBefore, await ReadRoleLinksAsync(fixture.Context, ids));
        Assert.Equal(permissionDefinitionsBefore, await ReadPermissionDefinitionsAsync(fixture.Context));
        Assert.Equal(roleDefinitionsBefore, await ReadRoleDefinitionsAsync(fixture.Context));
        Assert.Equal(roleGrantsBefore, await ReadRoleGrantsAsync(fixture.Context));
    }

    [Fact]
    public async Task CurrentSpaDefinesPublishedUsersAccessRoutesUnderAdminCanManageUsersGuard()
    {
        var repoRoot = RepositoryRoot();
        var appRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "app-routing.module.ts"));
        var adminRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "admin",
            "admin-routing.module.ts"));

        Assert.Contains("path: 'users'", appRouting);
        Assert.Contains("roles: ['Admin']", adminRouting);
        Assert.Contains("permissions: ['CanManageUsers']", adminRouting);

        string[] publishedChildPaths = ["list", "branding", "password-rules", "login-lockout"];
        foreach (var childPath in publishedChildPaths)
            Assert.Contains($"path: '{childPath}'", adminRouting);
    }

    [Fact]
    public async Task ContextualUserRoutesRemainOutOfMenuAndNoStandaloneDetailRouteIsDeclared()
    {
        var repoRoot = RepositoryRoot();
        var adminRouting = await File.ReadAllTextAsync(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "admin",
            "admin-routing.module.ts"));

        Assert.Contains("path: 'new'", adminRouting);
        Assert.Contains("path: ':id/edit'", adminRouting);
        Assert.Contains("path: ':id/roles'", adminRouting);
        Assert.DoesNotContain("path: ':id',", adminRouting);

        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();
        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        string[] contextualMenuRoutes =
        [
            "/users/new",
            "/users/:id",
            "/users/:id/edit",
            "/users/:id/roles"
        ];

        Assert.False(await fixture.Context.MenuItems
            .AsNoTracking()
            .AnyAsync(item => contextualMenuRoutes.Contains(item.Route)));
    }

    [Fact]
    public async Task CanonicalSeeder_LeavesOtherOutOfScopeRoutesUntouchedAndIsIdempotentForNav28AScope()
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

        var first = await ReadNav28ASnapshotAsync(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var second = await ReadNav28ASnapshotAsync(fixture.Context);
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

    private static async Task<string[]> ReadNav28ASnapshotAsync(AchDbContext context)
    {
        var routes = ExistingUsersAccessRoutes
            .Append(NavigationInformationArchitectureSeeder.AdministrationRoute)
            .ToArray();
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
