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
    public async Task NavigationMenu_ShouldUsePersistedSourceWithoutInjectingMissingRoutes()
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

        Assert.Single(menu);
        Assert.Equal(1, routes.Count(x => x == "/nacha-config-admin/perfiles"));
        Assert.Contains("/nacha-config-admin/perfiles", routes);
        Assert.DoesNotContain("/nacha-config-admin/records", routes);
        Assert.DoesNotContain("/nacha-config-admin/variants-fields", routes);
        Assert.DoesNotContain("/ach-cycles/nacha/layouts", routes);
        Assert.DoesNotContain("/ach-cycles/nacha/definitions", routes);
        Assert.DoesNotContain("/nacha-layouts", routes);
        Assert.DoesNotContain("/nacha-record-definitions", routes);
        Assert.DoesNotContain("Layouts NACHA", labels);
        Assert.DoesNotContain("Definiciones NACHA", labels);
        Assert.Contains("Config Profiles", labels);
    }

    [Fact]
    public async Task NavigationMenu_ShouldKeepOfficialNachaConfigGroupRouteAndProfileChildDistinct()
    {
        var readAch = new Permission { Id = PermissionConfiguration.ReadAchPermissionId, Name = "CanReadAch" };
        var repo = new FakeMenuQueryRepository([
            MenuItem(MenuItemConfiguration.NachaLayoutsId, "Configuración NACHA-M", "/nacha-config-admin", readAch),
            MenuItem(MenuItemConfiguration.NachaDefinitionsId, "Perfiles oficiales", "/nacha-config-admin/perfiles", readAch, MenuItemConfiguration.NachaLayoutsId),
            MenuItem(MenuItemConfiguration.NachaConfigRecordsId, "Registros oficiales", "/nacha-config-admin/records", readAch, MenuItemConfiguration.NachaLayoutsId),
            MenuItem(MenuItemConfiguration.NachaConfigVariantsFieldsId, "Variantes y campos", "/nacha-config-admin/variants-fields", readAch, MenuItemConfiguration.NachaLayoutsId)
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
        var group = Assert.Single(menu, x => x.Id == MenuItemConfiguration.NachaLayoutsId);
        var profiles = Assert.Single(group.Children, x => x.Id == MenuItemConfiguration.NachaDefinitionsId);
        var routes = FlattenRoutes(menu).ToList();

        Assert.Equal("/nacha-config-admin", group.Route);
        Assert.Equal("/nacha-config-admin/perfiles", profiles.Route);
        Assert.Equal(1, routes.Count(x => x == "/nacha-config-admin/perfiles"));
        Assert.Contains(group.Children, x =>
            x.Id == MenuItemConfiguration.NachaConfigRecordsId && x.Route == "/nacha-config-admin/records");
        Assert.Contains(group.Children, x =>
            x.Id == MenuItemConfiguration.NachaConfigVariantsFieldsId && x.Route == "/nacha-config-admin/variants-fields");
    }

    [Fact]
    public async Task NavigationMenu_ShouldHideUatRoutes_WhenFeatureGateIsUnavailable()
    {
        var readAch = new Permission { Id = PermissionConfiguration.ReadAchPermissionId, Name = "CanReadAch" };
        var repo = new FakeMenuQueryRepository([
            MenuItem(MenuItemConfiguration.UatSimulatorsId, "UAT", "/uat", readAch),
            MenuItem(MenuItemConfiguration.NachaInboundSimulatorId, "Simulador", "/uat/nacha-inbound-simulator", readAch, MenuItemConfiguration.UatSimulatorsId)
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

        var menu = await new GetMenuForCurrentUserHandler(repo, http)
            .Handle(new GetMenuForCurrentUserQuery(), CancellationToken.None);

        Assert.Empty(menu);
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

        Assert.Contains("/nacha-config-admin", activeRoutes);
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

        var group = await context.MenuItems.SingleAsync(x => x.Id == MenuItemConfiguration.NachaLayoutsId);
        var profiles = await context.MenuItems.SingleAsync(x => x.Id == MenuItemConfiguration.NachaDefinitionsId);
        var profileRouteCount = await context.MenuItems.CountAsync(x =>
            x.IsActive && x.Route == "/nacha-config-admin/perfiles");

        Assert.Equal("/nacha-config-admin", group.Route);
        Assert.Equal("/nacha-config-admin/perfiles", profiles.Route);
        Assert.Equal(MenuItemConfiguration.NachaLayoutsId, profiles.ParentId);
        Assert.Equal(1, profileRouteCount);

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

    [Fact]
    public async Task CyclesMenuSeeder_ShouldKeepOneCanonicalEntryAndDisableDuplicatesIdempotently()
    {
        await using var context = BuildContext();
        context.MenuItems.AddRange(
            new MenuItem { Id = 901, MenuId = MenuConfiguration.MainMenuId, Label = "Reglas de ciclos", Route = "/transactions/cycle-configs", Icon = "settings", Order = 90, Exact = true, IsActive = true },
            new MenuItem { Id = 902, MenuId = MenuConfiguration.MainMenuId, Label = "Ciclos CENIT", Route = "/cenit/operacion/ciclos", Icon = "schedule", Order = 91, Exact = true, IsActive = true });
        await context.SaveChangesAsync();

        await new CyclesMenuSeeder(context).SeedAsync();
        await new CyclesMenuSeeder(context).SeedAsync();

        var canonical = await context.MenuItems.SingleAsync(x => x.Id == MenuItemConfiguration.AchCyclesId);
        Assert.Equal("Configuración de ciclos", canonical.Label);
        Assert.Equal("/ach-cycles", canonical.Route);
        Assert.True(canonical.IsActive);
        Assert.False(await context.MenuItems.AnyAsync(x =>
            x.IsActive && (x.Route == "/transactions/cycle-configs" || x.Route == "/cenit/operacion/ciclos")));
        Assert.Equal(1, await context.MenuItemPermissions.CountAsync(x =>
            x.MenuItemId == MenuItemConfiguration.AchCyclesId
            && x.PermissionId == PermissionConfiguration.ReadAchPermissionId));
    }

    private static MenuItem MenuItem(int id, string label, string route, Permission permission, int? parentId = null)
    {
        return new MenuItem
        {
            Id = id,
            ParentId = parentId,
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
