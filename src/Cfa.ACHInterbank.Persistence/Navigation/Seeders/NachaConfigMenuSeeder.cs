using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Navigation.Seeders;

[Scoped]
public sealed class NachaConfigMenuSeeder : IDbSeeder
{
    private const string GroupLabel = "NACHA-M Configuración";
    private const string ProfilesLabel = "Perfiles oficiales";
    private const string RecordsLabel = "Records oficiales";
    private const string VariantsFieldsLabel = "Variants y Fields";
    private const string ProfilesRoute = "/nacha-config-admin/perfiles";
    private const string RecordsRoute = "/nacha-config-admin/records";
    private const string VariantsFieldsRoute = "/nacha-config-admin/variants-fields";
    private static readonly string[] LegacyRoutes = ["/ach-cycles/nacha/layouts", "/ach-cycles/nacha/definitions"];

    private readonly AchDbContext _context;

    public NachaConfigMenuSeeder(AchDbContext context)
    {
        _context = context;
    }

    public int Order => 1;

    public async Task SeedAsync()
    {
        await DisableLegacyMenuItemsAsync();

        var group = await UpsertMenuItemAsync(
            MenuItemConfiguration.NachaLayoutsId,
            parentId: null,
            label: GroupLabel,
            route: ProfilesRoute,
            icon: "tune",
            order: 2);

        await UpsertMenuItemAsync(
            MenuItemConfiguration.NachaDefinitionsId,
            group.Id,
            ProfilesLabel,
            ProfilesRoute,
            "fact_check",
            order: 1);

        await UpsertMenuItemAsync(
            MenuItemConfiguration.NachaConfigRecordsId,
            group.Id,
            RecordsLabel,
            RecordsRoute,
            "view_list",
            order: 2);

        await UpsertMenuItemAsync(
            MenuItemConfiguration.NachaConfigVariantsFieldsId,
            group.Id,
            VariantsFieldsLabel,
            VariantsFieldsRoute,
            "schema",
            order: 3);

        await EnsureReadAchPermissionAsync(
            MenuItemConfiguration.NachaLayoutsId,
            MenuItemConfiguration.NachaDefinitionsId,
            MenuItemConfiguration.NachaConfigRecordsId,
            MenuItemConfiguration.NachaConfigVariantsFieldsId);

        await RemoveRoleRestrictionsAsync(
            MenuItemConfiguration.NachaLayoutsId,
            MenuItemConfiguration.NachaDefinitionsId,
            MenuItemConfiguration.NachaConfigRecordsId,
            MenuItemConfiguration.NachaConfigVariantsFieldsId);

        await _context.SaveChangesAsync();
    }

    private async Task DisableLegacyMenuItemsAsync()
    {
        var legacyItems = await _context.MenuItems
            .Where(x => LegacyRoutes.Contains(x.Route))
            .ToListAsync();

        foreach (var item in legacyItems)
        {
            item.IsActive = false;
        }
    }

    private async Task<MenuItem> UpsertMenuItemAsync(
        int id,
        int? parentId,
        string label,
        string route,
        string icon,
        int order)
    {
        var item = await _context.MenuItems.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null)
        {
            item = new MenuItem { Id = id };
            _context.MenuItems.Add(item);
        }

        item.MenuId = MenuConfiguration.MainMenuId;
        item.ParentId = parentId;
        item.Label = label;
        item.Route = route;
        item.Icon = icon;
        item.Order = order;
        item.Exact = true;
        item.IsActive = true;

        return item;
    }

    private async Task EnsureReadAchPermissionAsync(params int[] menuItemIds)
    {
        var existingPermissions = await _context.MenuItemPermissions
            .Where(x => menuItemIds.Contains(x.MenuItemId))
            .ToListAsync();

        _context.MenuItemPermissions.RemoveRange(existingPermissions);

        foreach (var menuItemId in menuItemIds)
        {
            _context.MenuItemPermissions.Add(new MenuItemPermission
            {
                MenuItemId = menuItemId,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            });
        }
    }

    private async Task RemoveRoleRestrictionsAsync(params int[] menuItemIds)
    {
        var existingRoles = await _context.MenuItemRoles
            .Where(x => menuItemIds.Contains(x.MenuItemId))
            .ToListAsync();

        _context.MenuItemRoles.RemoveRange(existingRoles);
    }
}
