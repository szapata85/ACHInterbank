using System.Security.Claims;
using Cfa.ACHInterbank.Application.DataBase.Queries.Navigation;
using Cfa.ACHInterbank.Application.Navigation;
using Cfa.ACHInterbank.Application.Navigation.Queries;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class IncomingNachaCommandCenterMenuSeederTests
{
    [Fact]
    public async Task SeedAsync_ShouldCreateOneCanonicalOperationalEntry_AndRemainIdempotent()
    {
        await using var context = BuildContext();
        context.MenuItems.Add(TransactionsParent());
        await context.SaveChangesAsync();

        var seeder = new IncomingNachaCommandCenterMenuSeeder(context);
        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var item = await context.MenuItems.SingleAsync(candidate =>
            candidate.Route == IncomingNachaCommandCenterMenuSeeder.CanonicalRoute);

        Assert.Equal(IncomingNachaCommandCenterMenuSeeder.CanonicalLabel, item.Label);
        Assert.Equal(IncomingNachaCommandCenterMenuSeeder.CanonicalIcon, item.Icon);
        Assert.Equal(IncomingNachaCommandCenterMenuSeeder.CanonicalOrder, item.Order);
        Assert.Equal(MenuItemConfiguration.TransactionsId, item.ParentId);
        Assert.Equal(MenuConfiguration.MainMenuId, item.MenuId);
        Assert.True(item.Exact);
        Assert.True(item.IsActive);
        Assert.Equal(1, await context.MenuItemPermissions.CountAsync(link =>
            link.MenuItemId == item.Id
            && link.PermissionId == PermissionConfiguration.ReadAchPermissionId));
        Assert.Equal(2, await context.MenuItemRoles.CountAsync(link => link.MenuItemId == item.Id));
        Assert.True(await context.MenuItemRoles.AnyAsync(link =>
            link.MenuItemId == item.Id && link.RoleId == RoleConfiguration.AdminRoleId));
        Assert.True(await context.MenuItemRoles.AnyAsync(link =>
            link.MenuItemId == item.Id && link.RoleId == RoleConfiguration.OperatorRoleId));
    }

    [Fact]
    public async Task SeedAsync_ShouldUpgradeLegacyEntry_AndDisableEquivalentDuplicates()
    {
        await using var context = BuildContext();
        context.MenuItems.AddRange(
            TransactionsParent(),
            new MenuItem
            {
                Id = 9001,
                MenuId = MenuConfiguration.MainMenuId,
                Label = "Command Center inbound NACHA",
                Route = "/legacy/incoming-nacha",
                Icon = "memory",
                Order = 90,
                Exact = false,
                IsActive = true
            },
            new MenuItem
            {
                Id = 9002,
                MenuId = MenuConfiguration.MainMenuId,
                Label = "Inbound NACHA",
                Route = IncomingNachaCommandCenterMenuSeeder.CanonicalRoute,
                Icon = "terminal",
                Order = 91,
                Exact = false,
                IsActive = true
            });
        await context.SaveChangesAsync();

        var seeder = new IncomingNachaCommandCenterMenuSeeder(context);
        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var canonical = await context.MenuItems.SingleAsync(item =>
            item.Route == IncomingNachaCommandCenterMenuSeeder.CanonicalRoute);
        Assert.Equal(9002, canonical.Id);
        Assert.Equal(IncomingNachaCommandCenterMenuSeeder.CanonicalLabel, canonical.Label);
        Assert.True(canonical.IsActive);

        var duplicate = await context.MenuItems.SingleAsync(item => item.Id == 9001);
        Assert.False(duplicate.IsActive);
        Assert.False(await context.MenuItemPermissions.AnyAsync(link => link.MenuItemId == duplicate.Id));
        Assert.False(await context.MenuItemRoles.AnyAsync(link => link.MenuItemId == duplicate.Id));
        Assert.Equal(1, await context.MenuItems.CountAsync(item =>
            item.IsActive && item.Route == IncomingNachaCommandCenterMenuSeeder.CanonicalRoute));
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("ACH.Operator")]
    public async Task MenuHandler_ShouldExposeCanonicalEntry_ToAuthorizedRolesWithReadPermission(string role)
    {
        var menu = await GetMenuAsync(role, includeReadPermission: true);

        var transactions = Assert.Single(menu, item => item.Route == "/transactions");
        var commandCenter = Assert.Single(transactions.Children, item =>
            item.Route == IncomingNachaCommandCenterMenuSeeder.CanonicalRoute);
        Assert.Equal(IncomingNachaCommandCenterMenuSeeder.CanonicalLabel, commandCenter.Label);
    }

    [Fact]
    public async Task MenuHandler_ShouldNotExposeCanonicalEntry_WithoutReadPermission()
    {
        var menu = await GetMenuAsync("ACH.Operator", includeReadPermission: false);

        Assert.DoesNotContain(
            Flatten(menu),
            item => item.Route == IncomingNachaCommandCenterMenuSeeder.CanonicalRoute);
    }

    private static async Task<IList<MenuItemDto>> GetMenuAsync(string role, bool includeReadPermission)
    {
        var readPermission = new Permission
        {
            Id = PermissionConfiguration.ReadAchPermissionId,
            Name = "CanReadAch"
        };
        var roleEntity = new Role
        {
            Id = role == "Admin" ? RoleConfiguration.AdminRoleId : RoleConfiguration.OperatorRoleId,
            Name = role
        };
        var parent = TransactionsParent();
        var commandCenter = new MenuItem
        {
            Id = 9003,
            ParentId = parent.Id,
            Label = IncomingNachaCommandCenterMenuSeeder.CanonicalLabel,
            Route = IncomingNachaCommandCenterMenuSeeder.CanonicalRoute,
            Icon = IncomingNachaCommandCenterMenuSeeder.CanonicalIcon,
            Order = IncomingNachaCommandCenterMenuSeeder.CanonicalOrder,
            Exact = true,
            IsActive = true,
            MenuItemPermissions =
            {
                new MenuItemPermission
                {
                    MenuItemId = 9003,
                    PermissionId = readPermission.Id,
                    Permission = readPermission
                }
            },
            MenuItemRoles =
            {
                new MenuItemRole
                {
                    MenuItemId = 9003,
                    RoleId = roleEntity.Id,
                    Role = roleEntity
                }
            }
        };
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (includeReadPermission)
        {
            claims.Add(new Claim("permission", "CanReadAch"));
        }

        var handler = new GetMenuForCurrentUserHandler(
            new FakeMenuQueryRepository([parent, commandCenter]),
            new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
                }
            });

        return await handler.Handle(new GetMenuForCurrentUserQuery(), CancellationToken.None);
    }

    private static MenuItem TransactionsParent() => new()
    {
        Id = MenuItemConfiguration.TransactionsId,
        MenuId = MenuConfiguration.MainMenuId,
        Label = "Transacciones",
        Route = "/transactions",
        Icon = "swap_horiz",
        Order = 6,
        Exact = false,
        IsActive = true
    };

    private static IEnumerable<MenuItemDto> Flatten(IEnumerable<MenuItemDto> items)
        => items.SelectMany(item => new[] { item }.Concat(Flatten(item.Children)));

    private static AchDbContext BuildContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase($"incoming-command-center-menu-{Guid.NewGuid():N}")
            .Options);

    private sealed class FakeMenuQueryRepository(IEnumerable<MenuItem> items) : IMenuQueryRepository
    {
        public Task<List<MenuItem>> GetActiveMenuItemsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(items.Where(item => item.IsActive).ToList());
    }
}
