using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Navigation.Seeders;

[Scoped]
public sealed class AchColombiaMftAdministrationMenuSeeder(AchDbContext context) : IDbSeeder
{
    public const string Route = "/administracion/mft-ach";
    public int Order => 4;
    public async Task SeedAsync()
    {
        var parent = await context.MenuItems.SingleAsync(x => x.Route == "/transactions");
        var item = await context.MenuItems.SingleOrDefaultAsync(x => x.Route == Route) ?? new MenuItem();
        if (item.Id == 0) context.MenuItems.Add(item);
        item.MenuId = MenuConfiguration.MainMenuId; item.ParentId = parent.Id; item.Label = "MFT ACH"; item.Route = Route;
        item.Icon = "admin_panel_settings"; item.Order = 9; item.Exact = true; item.IsActive = true;
        await context.SaveChangesAsync();
        if (!await context.MenuItemPermissions.AnyAsync(x => x.MenuItemId == item.Id && x.PermissionId == PermissionConfiguration.ReadAchPermissionId))
            context.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, PermissionId = PermissionConfiguration.ReadAchPermissionId });
        if (!await context.MenuItemRoles.AnyAsync(x => x.MenuItemId == item.Id && x.RoleId == RoleConfiguration.AdminRoleId))
            context.MenuItemRoles.Add(new MenuItemRole { MenuItemId = item.Id, RoleId = RoleConfiguration.AdminRoleId });
        await context.SaveChangesAsync();
    }
}
