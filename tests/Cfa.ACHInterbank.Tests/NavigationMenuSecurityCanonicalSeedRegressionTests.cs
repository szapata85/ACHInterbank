using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// Regression guard: a fresh database and repeated functional seeding must not
/// place file security before Inicio or restore the obsolete 'Seguridad NACHA' label.
/// </summary>
public sealed class NavigationMenuSecurityCanonicalSeedRegressionTests
{
    [Fact]
    public async Task FreshDatabase_FunctionalSeedPlacesFileSecurityAfterHome()
    {
        // EF HasData preserves the historical migration baseline. The application
        // must run the functional/canonical seed after applying migrations.
        await using var fixture = CreateFixture();
        await new NachaSecurityMenuSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var home = await fixture.Context.MenuItems.AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.DashboardId);
        var security = await fixture.Context.MenuItems.AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.NachaSecurityId);

        Assert.Equal("/dashboard", home.Route);
        Assert.Null(home.ParentId);
        Assert.Equal(NavigationInformationArchitectureSeeder.FileSecurityRoute, security.Route);
        Assert.Equal(NavigationInformationArchitectureSeeder.FileSecurityLabel, security.Label);
        Assert.Equal(NavigationInformationArchitectureSeeder.FileSecurityRootOrder, security.Order);
        Assert.Null(security.ParentId);
        Assert.True(home.Order < security.Order);
    }

    [Fact]
    public async Task FunctionalSeed_RepairsLegacyRow_AndRepeatedExecutionDoesNotCreateDuplicates()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        var security = await context.MenuItems.SingleAsync(item => item.Id == MenuItemConfiguration.NachaSecurityId);
        security.Label = "Seguridad NACHA";
        security.Order = 8;
        security.Route = "/nacha-security";
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var seeder = new NachaSecurityMenuSeeder(context);
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();
        var first = await ReadFileSecuritySnapshotAsync(context);

        await seeder.SeedAsync();
        context.ChangeTracker.Clear();
        var second = await ReadFileSecuritySnapshotAsync(context);
        Assert.Equal(first, second);

        var root = await context.MenuItems.AsNoTracking().SingleAsync(item => item.Id == MenuItemConfiguration.NachaSecurityId);
        Assert.Equal(NavigationInformationArchitectureSeeder.FileSecurityLabel, root.Label);
        Assert.Equal(NavigationInformationArchitectureSeeder.FileSecurityRootOrder, root.Order);
        Assert.True(root.IsActive);
        Assert.Null(root.ParentId);
        Assert.Single(await context.MenuItems.AsNoTracking()
            .Where(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.FileSecurityRoute)
            .ToListAsync());

        var children = await context.MenuItems.AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == root.Id)
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order })
            .ToArrayAsync();
        Assert.Equal(2, children.Length);
        Assert.Equal(("/nacha-security/certificates", "Certificados digitales", 10),
            (children[0].Route, children[0].Label, children[0].Order));
        Assert.Equal(("/nacha-security/sobre-digital", "Cifrar y descifrar archivos", 20),
            (children[1].Route, children[1].Label, children[1].Order));
    }

    private static async Task<string[]> ReadFileSecuritySnapshotAsync(AchDbContext context)
    {
        var rootsAndChildren = await context.MenuItems.AsNoTracking()
            .Where(item => item.Id == MenuItemConfiguration.NachaSecurityId
                || item.ParentId == MenuItemConfiguration.NachaSecurityId)
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.ParentId, item.Route, item.Label, item.Order, item.IsActive })
            .ToListAsync();
        var ids = rootsAndChildren.Select(item => item.Id).ToArray();
        var roles = await context.MenuItemRoles.AsNoTracking()
            .Where(item => ids.Contains(item.MenuItemId))
            .OrderBy(item => item.MenuItemId).ThenBy(item => item.RoleId)
            .Select(item => new { item.MenuItemId, item.RoleId }).ToArrayAsync();
        var permissions = await context.MenuItemPermissions.AsNoTracking()
            .Where(item => ids.Contains(item.MenuItemId))
            .OrderBy(item => item.MenuItemId).ThenBy(item => item.PermissionId)
            .Select(item => new { item.MenuItemId, item.PermissionId }).ToArrayAsync();

        return rootsAndChildren.Select(item => $"Item|{item.Id}|{item.ParentId}|{item.Route}|{item.Label}|{item.Order}|{item.IsActive}")
            .Concat(roles.Select(item => $"Role|{item.MenuItemId}|{item.RoleId}"))
            .Concat(permissions.Select(item => $"Permission|{item.MenuItemId}|{item.PermissionId}"))
            .ToArray();
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
