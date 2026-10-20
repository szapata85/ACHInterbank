using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Aligns the canonical menu entry for Organización del menú with the semantic
/// NavigationMenu.Manage capability while preserving the current audience through
/// the validated CanManageUsers fallback. Direct MenuItemRole gating is removed so
/// menu visibility has the same OR semantics as SPA/API authorization.
/// </summary>
[Scoped]
public sealed class NavigationMenuAuthorizationSeeder(AchDbContext context) : IDbSeeder
{
    public const int AuthorizationReconciliationOrder = 108;

    public int Order => AuthorizationReconciliationOrder;

    public async Task SeedAsync()
    {
        await EnsurePermissionAsync(
            NavigationMenuPermissionSeeder.PermissionId,
            FineGrainedPermissions.NavigationMenu.Manage,
            "Administrar organización del menú");
        await EnsurePermissionAsync(
            PermissionConfiguration.ManageUsersPermissionId,
            "CanManageUsers",
            "Gestión de usuarios");

        var menuItem = await context.MenuItems
            .SingleAsync(item => item.Id == MenuItemConfiguration.NavigationAdminId);

        if (!string.Equals(menuItem.Route, NavigationInformationArchitectureSeeder.MenuOrganizationRoute, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected menu-organization route '{NavigationInformationArchitectureSeeder.MenuOrganizationRoute}' before NAV3.4H authorization reconciliation.");
        }

        await ReconcilePermissionsAsync(menuItem.Id,
        [
            NavigationMenuPermissionSeeder.PermissionId,
            PermissionConfiguration.ManageUsersPermissionId
        ]);

        var roleLinks = await context.MenuItemRoles
            .Where(link => link.MenuItemId == menuItem.Id)
            .ToListAsync();
        context.MenuItemRoles.RemoveRange(roleLinks);

        await context.SaveChangesAsync();
    }

    private async Task EnsurePermissionAsync(Guid id, string name, string description)
    {
        var permission = await context.Permissions
            .FirstOrDefaultAsync(item => item.Name == name)
            ?? await context.Permissions.FindAsync(id);

        if (permission is null)
        {
            permission = new Permission { Id = id };
            context.Permissions.Add(permission);
        }

        permission.Name = name;
        permission.Description = description;
    }

    private async Task ReconcilePermissionsAsync(int menuItemId, IReadOnlyCollection<Guid> desiredPermissionIds)
    {
        var current = await context.MenuItemPermissions
            .Where(link => link.MenuItemId == menuItemId)
            .ToListAsync();

        context.MenuItemPermissions.RemoveRange(
            current.Where(link => !desiredPermissionIds.Contains(link.PermissionId)));

        var currentIds = current.Select(link => link.PermissionId).ToHashSet();
        foreach (var permissionId in desiredPermissionIds.Where(id => !currentIds.Contains(id)))
        {
            context.MenuItemPermissions.Add(new MenuItemPermission
            {
                MenuItemId = menuItemId,
                PermissionId = permissionId
            });
        }
    }
}
