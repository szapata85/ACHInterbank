using System.Security.Claims;
using Cfa.ACHInterbank.Application.DataBase.Queries.Navigation;
using Cfa.ACHInterbank.Application.Navigation.Queries;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NavigationMenuNachaConfigTests
{
    [Fact]
    public async Task NavigationMenu_ShouldExposeOfficialNachaConfigAndHideLegacyRoutes()
    {
        var readAch = new Permission { Id = PermissionConfiguration.ReadAchPermissionId, Name = "CanReadAch" };
        var repo = new FakeMenuQueryRepository([
            MenuItem(1, "Layouts NACHA", "/ach-cycles/nacha/layouts", readAch),
            MenuItem(2, "Definiciones NACHA", "/ach-cycles/nacha/definitions", readAch),
            MenuItem(20, "Config Profiles", "/nacha-config-admin/perfiles", readAch)
        ]);
        var http = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim("permission", "CanReadAch")
                ], "test"))
            }
        };
        var handler = new GetMenuForCurrentUserHandler(repo, http);

        var menu = await handler.Handle(new GetMenuForCurrentUserQuery(), CancellationToken.None);
        var routes = FlattenRoutes(menu).ToList();
        var labels = FlattenLabels(menu).ToList();

        Assert.Contains("/nacha-config-admin/perfiles", routes);
        Assert.Contains("/nacha-config-admin/records", routes);
        Assert.Contains("/nacha-config-admin/variants-fields", routes);
        Assert.DoesNotContain("/ach-cycles/nacha/layouts", routes);
        Assert.DoesNotContain("/ach-cycles/nacha/definitions", routes);
        Assert.DoesNotContain("/nacha-layouts", routes);
        Assert.DoesNotContain("/nacha-record-definitions", routes);
        Assert.DoesNotContain("Layouts NACHA", labels);
        Assert.DoesNotContain("Definiciones NACHA", labels);
        Assert.Contains("Configuración NACHA-M", labels);
        Assert.Contains("Registros oficiales", labels);
        Assert.Contains("Variantes y campos", labels);
    }

    [Fact]
    public async Task NachaConfigMenuSeeder_ShouldRepairPersistedLegacyMenu()
    {
        await using var context = BuildContext();
        context.MenuItems.AddRange(
            new MenuItem { Id = 20, MenuId = MenuConfiguration.MainMenuId, Label = "Layouts NACHA", Route = "/ach-cycles/nacha/layouts", Icon = "view_column", Order = 2, Exact = true, IsActive = true },
            new MenuItem { Id = 25, MenuId = MenuConfiguration.MainMenuId, Label = "Definiciones NACHA", Route = "/ach-cycles/nacha/definitions", Icon = "playlist_add_check", Order = 3, Exact = true, IsActive = true });
        context.MenuItemPermissions.AddRange(
            new MenuItemPermission { MenuItemId = 20, PermissionId = PermissionConfiguration.ManageAchPermissionId },
            new MenuItemPermission { MenuItemId = 25, PermissionId = PermissionConfiguration.ManageAchPermissionId });
        context.MenuItemRoles.Add(new MenuItemRole { MenuItemId = 25, RoleId = RoleConfiguration.AdminRoleId });
        await context.SaveChangesAsync();

        await new NachaConfigMenuSeeder(context).SeedAsync();
        await new NachaConfigMenuSeeder(context).SeedAsync();

        var activeRoutes = await context.MenuItems
            .Where(x => x.IsActive)
            .Select(x => x.Route)
            .ToListAsync();
        var labels = await context.MenuItems
            .Where(x => x.IsActive)
            .Select(x => x.Label)
            .ToListAsync();

        Assert.Contains("/nacha-config-admin/perfiles", activeRoutes);
        Assert.Contains("/nacha-config-admin/records", activeRoutes);
        Assert.Contains("/nacha-config-admin/variants-fields", activeRoutes);
        Assert.DoesNotContain("/ach-cycles/nacha/layouts", activeRoutes);
        Assert.DoesNotContain("/ach-cycles/nacha/definitions", activeRoutes);
        Assert.DoesNotContain("/nacha-layouts", activeRoutes);
        Assert.DoesNotContain("/nacha-record-definitions", activeRoutes);
        Assert.DoesNotContain("Layouts NACHA", labels);
        Assert.DoesNotContain("Definiciones NACHA", labels);
        Assert.Contains("Configuración NACHA-M", labels);
        Assert.Contains("Registros oficiales", labels);
        Assert.Contains("Variantes y campos", labels);

        var officialIds = new[]
        {
            MenuItemConfiguration.NachaLayoutsId,
            MenuItemConfiguration.NachaDefinitionsId,
            MenuItemConfiguration.NachaConfigRecordsId,
            MenuItemConfiguration.NachaConfigVariantsFieldsId
        };
        foreach (var id in officialIds)
        {
            Assert.True(await context.MenuItemPermissions.AnyAsync(x => x.MenuItemId == id && x.PermissionId == PermissionConfiguration.ReadAchPermissionId));
        }
        Assert.False(await context.MenuItemRoles.AnyAsync(x => officialIds.Contains(x.MenuItemId)));
    }

    private static MenuItem MenuItem(int id, string label, string route, Permission permission)
    {
        return new MenuItem
        {
            Id = id,
            Label = label,
            Route = route,
            Icon = "menu",
            Exact = true,
            IsActive = true,
            MenuItemPermissions =
            {
                new MenuItemPermission { MenuItemId = id, PermissionId = permission.Id, Permission = permission }
            }
        };
    }

    private static IEnumerable<string> FlattenRoutes(IEnumerable<Application.Navigation.MenuItemDto> items)
        => items.SelectMany(x => new[] { x.Route }.Concat(FlattenRoutes(x.Children)));

    private static IEnumerable<string> FlattenLabels(IEnumerable<Application.Navigation.MenuItemDto> items)
        => items.SelectMany(x => new[] { x.Label }.Concat(FlattenLabels(x.Children)));

    private static AchDbContext BuildContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class FakeMenuQueryRepository : IMenuQueryRepository
    {
        private readonly List<MenuItem> _items;

        public FakeMenuQueryRepository(IEnumerable<MenuItem> items)
        {
            _items = items.ToList();
        }

        public Task<List<MenuItem>> GetActiveMenuItemsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Where(x => x.IsActive).ToList());
    }
}
