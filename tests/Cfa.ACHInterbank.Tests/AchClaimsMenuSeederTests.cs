using System.Security.Claims;
using Cfa.ACHInterbank.Application.DataBase.Queries.Navigation;
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

public sealed class AchClaimsMenuSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesReclamacionesAchUnderTransactions_WithClaimsReadOnly()
    {
        await using var fixture = CreateFixture();
        await new ClaimsPermissionSeeder(fixture.Context).SeedAsync();
        var seeder = new AchClaimsMenuSeeder(fixture.Context);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var item = await fixture.Context.MenuItems.SingleAsync(x => x.Route == AchClaimsMenuSeeder.CanonicalRoute);
        Assert.Equal(AchClaimsMenuSeeder.CanonicalLabel, item.Label);
        Assert.Equal(MenuItemConfiguration.TransactionsId, item.ParentId);
        Assert.True(item.IsActive);
        Assert.Equal(1, await fixture.Context.MenuItemPermissions.CountAsync(x => x.MenuItemId == item.Id));
        Assert.Equal(0, await fixture.Context.MenuItemRoles.CountAsync(x => x.MenuItemId == item.Id));
        Assert.True(await fixture.Context.MenuItemPermissions.AnyAsync(x =>
            x.MenuItemId == item.Id && x.PermissionId == AchClaimsMenuSeeder.ClaimsReadPermissionId));
        Assert.False(await fixture.Context.MenuItemPermissions.AnyAsync(x =>
            x.MenuItemId == item.Id && x.PermissionId == PermissionConfiguration.ReadAchPermissionId));
        Assert.True(await fixture.Context.MenuItemPermissions.AnyAsync(x =>
            x.MenuItemId == MenuItemConfiguration.TransactionsId && x.PermissionId == AchClaimsMenuSeeder.ClaimsReadPermissionId));
        Assert.Equal(1, await fixture.Context.Permissions.CountAsync(x => x.Name == FineGrainedPermissions.Claims.Read));
    }

    [Fact]
    public async Task Navigation_IsVisibleWithClaimsReadPermission()
    {
        var menu = await BuildMenuAsync(FineGrainedPermissions.Claims.Read);
        Assert.Contains(Flatten(menu), item => item.Route == AchClaimsMenuSeeder.CanonicalRoute);
    }


    [Fact]
    public async Task Navigation_IsNotVisibleWithClaimsCreateDev14PermissionOnly()
    {
        var menu = await BuildMenuAsync(FineGrainedPermissions.Claims.CreateDev14);
        Assert.DoesNotContain(Flatten(menu), item => item.Route == AchClaimsMenuSeeder.CanonicalRoute);
    }

    [Theory]
    [InlineData("CanReadAch")]
    [InlineData("CanManageAch")]
    public async Task Navigation_IsNotVisibleWithGeneralAchPermissionOnly(string permission)
    {
        var menu = await BuildMenuAsync(permission);
        Assert.DoesNotContain(Flatten(menu), item => item.Route == AchClaimsMenuSeeder.CanonicalRoute);
    }

    private static async Task<IList<MenuItemDto>> BuildMenuAsync(string permission)
    {
        await using var fixture = CreateFixture();
        await new ClaimsPermissionSeeder(fixture.Context).SeedAsync();
        await new AchClaimsMenuSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var handler = new GetMenuForCurrentUserHandler(
            new MenuQueryRepository(fixture.Context),
            new TestCurrentUserContext(
            [
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("permission", permission)
            ]));

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
