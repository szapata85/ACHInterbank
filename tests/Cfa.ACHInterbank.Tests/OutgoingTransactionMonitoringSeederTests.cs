using System.Security.Claims;
using Cfa.ACHInterbank.Application.DataBase.Queries.Navigation;
using Cfa.ACHInterbank.Application.Navigation;
using Cfa.ACHInterbank.Application.Navigation.Queries;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.DataBase.Repositories.Navigation;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class OutgoingTransactionMonitoringSeederTests
{
    [Fact]
    public async Task SeedAsync_ThreeExecutions_PreserveOneCanonicalMenuAndPermissionGraph()
    {
        await using var fixture = CreateFixture();
        var seeder = new OutgoingTransactionMonitoringSeeder(fixture.Context);

        await seeder.SeedAsync();
        var originalId = await fixture.Context.MenuItems
            .Where(item => item.Route == OutgoingTransactionMonitoringSeeder.CanonicalRoute)
            .Select(item => item.Id)
            .SingleAsync();

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var item = await fixture.Context.MenuItems.SingleAsync(candidate =>
            candidate.Route == OutgoingTransactionMonitoringSeeder.CanonicalRoute);
        Assert.Equal(originalId, item.Id);
        Assert.Equal(OutgoingTransactionMonitoringSeeder.CanonicalLabel, item.Label);
        Assert.Equal(OutgoingTransactionMonitoringSeeder.CanonicalIcon, item.Icon);
        Assert.Equal(OutgoingTransactionMonitoringSeeder.CanonicalOrder, item.Order);
        Assert.Equal(MenuItemConfiguration.TransactionsId, item.ParentId);
        Assert.Equal(MenuConfiguration.MainMenuId, item.MenuId);
        Assert.True(item.Exact);
        Assert.True(item.IsActive);

        Assert.Equal(1, await fixture.Context.Permissions.CountAsync(permission =>
            permission.Name == FineGrainedPermissions.OutgoingTransactions.MonitorRead));
        Assert.Equal(1, await fixture.Context.Permissions.CountAsync(permission =>
            permission.Name == FineGrainedPermissions.OutgoingTransactions.MonitorTechnicalDetailRead));
        Assert.Equal(1, await fixture.Context.MenuItemPermissions.CountAsync(link => link.MenuItemId == item.Id));
        Assert.True(await fixture.Context.MenuItemPermissions.AnyAsync(link =>
            link.MenuItemId == item.Id && link.PermissionId == OutgoingTransactionMonitoringSeeder.ReadPermissionId));
        Assert.Equal(2, await fixture.Context.MenuItemRoles.CountAsync(link => link.MenuItemId == item.Id));
        Assert.True(await fixture.Context.RolePermissions.AnyAsync(link =>
            link.RoleId == RoleConfiguration.AdminRoleId
            && link.PermissionId == OutgoingTransactionMonitoringSeeder.ReadPermissionId));
        Assert.True(await fixture.Context.RolePermissions.AnyAsync(link =>
            link.RoleId == RoleConfiguration.OperatorRoleId
            && link.PermissionId == OutgoingTransactionMonitoringSeeder.ReadPermissionId));
        Assert.True(await fixture.Context.RolePermissions.AnyAsync(link =>
            link.RoleId == RoleConfiguration.AdminRoleId
            && link.PermissionId == OutgoingTransactionMonitoringSeeder.TechnicalPermissionId));
        Assert.False(await fixture.Context.RolePermissions.AnyAsync(link =>
            link.RoleId == RoleConfiguration.OperatorRoleId
            && link.PermissionId == OutgoingTransactionMonitoringSeeder.TechnicalPermissionId));
    }

    [Fact]
    public async Task SeedAsync_UpdatesExistingItemWithoutChangingItsId_AndDisablesDuplicate()
    {
        await using var fixture = CreateFixture();
        var existing = new MenuItem
        {
            Label = "Monitor desactualizado",
            Route = OutgoingTransactionMonitoringSeeder.CanonicalRoute,
            Icon = "terminal",
            Order = 99,
            Exact = false,
            IsActive = false
        };
        var duplicate = new MenuItem
        {
            Label = OutgoingTransactionMonitoringSeeder.CanonicalLabel,
            Route = "/transactions/legacy-outgoing-monitor",
            Icon = "terminal",
            Order = 98,
            Exact = false,
            IsActive = true
        };
        fixture.Context.MenuItems.AddRange(existing, duplicate);
        await fixture.Context.SaveChangesAsync();
        var existingId = existing.Id;

        fixture.Context.MenuItemPermissions.Add(new MenuItemPermission
        {
            MenuItemId = duplicate.Id,
            PermissionId = PermissionConfiguration.ReadAchPermissionId
        });
        await fixture.Context.SaveChangesAsync();

        var seeder = new OutgoingTransactionMonitoringSeeder(fixture.Context);
        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var canonical = await fixture.Context.MenuItems.SingleAsync(item =>
            item.Route == OutgoingTransactionMonitoringSeeder.CanonicalRoute);
        Assert.Equal(existingId, canonical.Id);
        Assert.Equal(OutgoingTransactionMonitoringSeeder.CanonicalLabel, canonical.Label);
        Assert.Equal(MenuItemConfiguration.TransactionsId, canonical.ParentId);
        Assert.True(canonical.IsActive);

        var disabled = await fixture.Context.MenuItems.SingleAsync(item => item.Id == duplicate.Id);
        Assert.False(disabled.IsActive);
        Assert.StartsWith("/navigation/disabled/outgoing-monitoring/", disabled.Route);
        Assert.False(await fixture.Context.MenuItemPermissions.AnyAsync(link => link.MenuItemId == duplicate.Id));
        Assert.False(await fixture.Context.MenuItemRoles.AnyAsync(link => link.MenuItemId == duplicate.Id));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task Navigation_RequiresTheFunctionalPermission(bool includePermission, bool expectedVisible)
    {
        await using var fixture = CreateFixture();
        await new OutgoingTransactionMonitoringSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "Admin"),
            new("permission", "CanReadAch")
        };
        if (includePermission)
            claims.Add(new Claim("permission", FineGrainedPermissions.OutgoingTransactions.MonitorRead));

        var handler = new GetMenuForCurrentUserHandler(
            new MenuQueryRepository(fixture.Context),
            new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
                }
            });

        var menu = await handler.Handle(new GetMenuForCurrentUserQuery(), CancellationToken.None);
        var visible = Flatten(menu).Any(item =>
            item.Route == OutgoingTransactionMonitoringSeeder.CanonicalRoute);
        Assert.Equal(expectedVisible, visible);
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
