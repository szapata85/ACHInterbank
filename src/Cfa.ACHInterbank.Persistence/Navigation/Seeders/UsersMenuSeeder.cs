using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Navigation.Seeders;

[Scoped]
public sealed class UsersMenuSeeder : IDbSeeder
{
    private const string UsersRoute = "/users";
    private const string ManageUsersRoute = "/users/list";

    private readonly AchDbContext _context;

    public UsersMenuSeeder(AchDbContext context) => _context = context;

    public int Order => 1;

    public async Task SeedAsync()
    {
        var users = await _context.MenuItems.FirstOrDefaultAsync(item => item.Id == MenuItemConfiguration.UsersId)
            ?? await _context.MenuItems.FirstOrDefaultAsync(item => item.Route == UsersRoute);

        if (users is null)
        {
            users = new MenuItem { Id = MenuItemConfiguration.UsersId };
            _context.MenuItems.Add(users);
        }

        users.MenuId = MenuConfiguration.MainMenuId;
        users.Label = "Usuarios";
        users.Route = UsersRoute;
        users.Icon = "group";
        users.Order = 2;
        users.Exact = false;
        users.IsActive = true;

        var manageUsers = await _context.MenuItems
            .FirstOrDefaultAsync(item => item.ParentId == users.Id && item.Route == ManageUsersRoute);

        if (manageUsers is null)
        {
            manageUsers = new MenuItem();
            _context.MenuItems.Add(manageUsers);
        }

        manageUsers.MenuId = MenuConfiguration.MainMenuId;
        manageUsers.ParentId = users.Id;
        manageUsers.Label = "Administrar usuarios";
        manageUsers.Route = ManageUsersRoute;
        manageUsers.Icon = "manage_accounts";
        manageUsers.Order = 1;
        manageUsers.Exact = true;
        manageUsers.IsActive = true;

        await _context.SaveChangesAsync();
        await EnsureAccessAsync(users.Id);
        await EnsureAccessAsync(manageUsers.Id);
        await _context.SaveChangesAsync();
    }

    private async Task EnsureAccessAsync(int menuItemId)
    {
        if (!await _context.MenuItemRoles.AnyAsync(item => item.MenuItemId == menuItemId && item.RoleId == RoleConfiguration.AdminRoleId))
        {
            _context.MenuItemRoles.Add(new MenuItemRole { MenuItemId = menuItemId, RoleId = RoleConfiguration.AdminRoleId });
        }

        if (!await _context.MenuItemPermissions.AnyAsync(item => item.MenuItemId == menuItemId && item.PermissionId == PermissionConfiguration.ManageUsersPermissionId))
        {
            _context.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = menuItemId, PermissionId = PermissionConfiguration.ManageUsersPermissionId });
        }
    }
}
