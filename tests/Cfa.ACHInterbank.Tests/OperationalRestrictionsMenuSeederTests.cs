using System.Security.Claims;
using Cfa.ACHInterbank.Application.Navigation;
using Cfa.ACHInterbank.Application.Navigation.Queries;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.DataBase.Repositories.Navigation;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class OperationalRestrictionsMenuSeederTests
{
    [Fact]
    public async Task SeedAsync_RegistersOneCatalogEntry_ProtectedOnlyByReadPermission()
    {
        await using var fixture = CreateFixture();
        await new CfaOperationalTransactionRestrictionsPermissionSeeder(fixture.Context).SeedAsync();
        var seeder = new OperationalRestrictionsMenuSeeder(fixture.Context);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var item = await fixture.Context.MenuItems.SingleAsync(candidate =>
            candidate.Route == OperationalRestrictionsMenuSeeder.CanonicalRoute);
        var readPermission = await fixture.Context.Permissions.SingleAsync(permission =>
            permission.Name == FineGrainedPermissions.OperationalRestrictions.Read);

        Assert.Equal(OperationalRestrictionsMenuSeeder.CanonicalLabel, item.Label);
        Assert.Equal(OperationalRestrictionsMenuSeeder.CanonicalIcon, item.Icon);
        Assert.Equal(OperationalRestrictionsMenuSeeder.CanonicalOrder, item.Order);
        Assert.Equal(MenuItemConfiguration.CatalogsId, item.ParentId);
        Assert.True(item.Exact);
        Assert.True(item.IsActive);
        Assert.Equal(1, await fixture.Context.MenuItemPermissions.CountAsync(link =>
            link.MenuItemId == item.Id && link.PermissionId == readPermission.Id));
        Assert.False(await fixture.Context.MenuItemPermissions.AnyAsync(link =>
            link.MenuItemId == item.Id && link.PermissionId != readPermission.Id));
        Assert.False(await fixture.Context.MenuItemRoles.AnyAsync(link => link.MenuItemId == item.Id));
        Assert.True(await fixture.Context.MenuItemPermissions.AnyAsync(link =>
            link.MenuItemId == MenuItemConfiguration.CatalogsId && link.PermissionId == readPermission.Id));
    }

    [Fact]
    public async Task Navigation_WithOperationalRestrictionsRead_ShowsEntryUnderCatalogs()
    {
        await using var fixture = CreateFixture();
        await SeedNavigationAsync(fixture.Context);

        var menu = await GetMenuAsync(
            fixture.Context,
            new Claim("permission", FineGrainedPermissions.OperationalRestrictions.Read));

        var catalogs = Assert.Single(menu, item => item.Route == "/catalogs");
        var entry = Assert.Single(catalogs.Children, item =>
            item.Route == OperationalRestrictionsMenuSeeder.CanonicalRoute);
        Assert.Equal(OperationalRestrictionsMenuSeeder.CanonicalLabel, entry.Label);
    }

    [Fact]
    public async Task Navigation_WithoutOperationalRestrictionsRead_HidesEntry()
    {
        await using var fixture = CreateFixture();
        await SeedNavigationAsync(fixture.Context);

        var menu = await GetMenuAsync(
            fixture.Context,
            new Claim("permission", "CanReadCatalogs"));

        Assert.DoesNotContain(Flatten(menu), item =>
            item.Route == OperationalRestrictionsMenuSeeder.CanonicalRoute);
    }

    private static async Task SeedNavigationAsync(AchDbContext context)
    {
        await new CfaOperationalTransactionRestrictionsPermissionSeeder(context).SeedAsync();
        await new OperationalRestrictionsMenuSeeder(context).SeedAsync();
        context.ChangeTracker.Clear();
    }

    private static async Task<IList<MenuItemDto>> GetMenuAsync(AchDbContext context, params Claim[] claims)
    {
        var handler = new GetMenuForCurrentUserHandler(
            new MenuQueryRepository(context),
            new TestCurrentUserContext(claims));

        return await handler.Handle(new GetMenuForCurrentUserQuery(), CancellationToken.None);
    }

    private static IEnumerable<MenuItemDto> Flatten(IEnumerable<MenuItemDto> items)
        => items.SelectMany(item => new[] { item }.Concat(Flatten(item.Children)));

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
