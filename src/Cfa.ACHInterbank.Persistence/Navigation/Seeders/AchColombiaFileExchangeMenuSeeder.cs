using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Navigation.Seeders;

[Scoped]
public sealed class AchColombiaFileExchangeMenuSeeder(AchDbContext context) : IDbSeeder
{
    public const string Label = "Intercambio de archivos ACH Colombia";
    public const string Route = "/ach-colombia/file-exchange";
    public int Order => 4;

    public async Task SeedAsync()
    {
        var parent = await context.MenuItems.SingleAsync(x => x.Route == "/transactions");
        var item = await context.MenuItems.SingleOrDefaultAsync(x => x.Route == Route);
        if (item is null)
        {
            item = new MenuItem();
            context.MenuItems.Add(item);
        }
        item.MenuId = MenuConfiguration.MainMenuId;
        item.ParentId = parent.Id;
        item.Label = Label;
        item.Route = Route;
        item.Icon = "sync_alt";
        item.Order = 8;
        item.Exact = true;
        item.IsActive = true;
        await context.SaveChangesAsync();

        if (!await context.MenuItemPermissions.AnyAsync(x => x.MenuItemId == item.Id && x.PermissionId == PermissionConfiguration.ReadAchPermissionId))
            context.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, PermissionId = PermissionConfiguration.ReadAchPermissionId });
        foreach (var roleId in new[] { RoleConfiguration.AdminRoleId, RoleConfiguration.OperatorRoleId })
            if (!await context.MenuItemRoles.AnyAsync(x => x.MenuItemId == item.Id && x.RoleId == roleId))
                context.MenuItemRoles.Add(new MenuItemRole { MenuItemId = item.Id, RoleId = roleId });
        await context.SaveChangesAsync();
    }
}
