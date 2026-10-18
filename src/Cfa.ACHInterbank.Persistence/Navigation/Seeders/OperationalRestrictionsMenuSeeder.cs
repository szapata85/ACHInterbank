using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Navigation.Seeders;

[Scoped]
public sealed class OperationalRestrictionsMenuSeeder(AchDbContext context) : IDbSeeder
{
    public const string CanonicalRoute = "/operational-restrictions";
    public const string CanonicalLabel = "Restricciones operacionales CFA";
    public const string CanonicalIcon = "rule_settings";
    public const int CanonicalOrder = 6;

    // Fine-grained permissions are seeded before navigation entries.
    public int Order => 1;

    public async Task SeedAsync()
    {
        var readPermission = await context.Permissions.SingleOrDefaultAsync(permission =>
            permission.Name == FineGrainedPermissions.OperationalRestrictions.Read)
            ?? throw new InvalidOperationException(
                $"Permission '{FineGrainedPermissions.OperationalRestrictions.Read}' must be seeded before the navigation entry.");

        var catalogs = await context.MenuItems.SingleOrDefaultAsync(item =>
            item.Id == MenuItemConfiguration.CatalogsId)
            ?? await context.MenuItems.SingleAsync(item => item.Route == "/catalogs");

        var candidates = await context.MenuItems
            .Where(item => item.Route == CanonicalRoute)
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.Id)
            .ToListAsync();

        var item = candidates.FirstOrDefault();
        if (item is null)
        {
            item = new MenuItem();
            context.MenuItems.Add(item);
        }

        item.MenuId = MenuConfiguration.MainMenuId;
        item.ParentId = catalogs.Id;
        item.Label = CanonicalLabel;
        item.Route = CanonicalRoute;
        item.Icon = CanonicalIcon;
        item.Order = CanonicalOrder;
        item.Exact = true;
        item.IsActive = true;

        foreach (var duplicate in candidates.Skip(1))
        {
            duplicate.IsActive = false;
            duplicate.Route = $"/navigation/disabled/operational-restrictions/{duplicate.Id}";
        }

        await context.SaveChangesAsync();

        var affectedIds = candidates.Select(candidate => candidate.Id).Append(item.Id).Distinct().ToArray();
        var permissionLinks = await context.MenuItemPermissions
            .Where(link => affectedIds.Contains(link.MenuItemId))
            .ToListAsync();
        var roleLinks = await context.MenuItemRoles
            .Where(link => affectedIds.Contains(link.MenuItemId))
            .ToListAsync();

        context.MenuItemPermissions.RemoveRange(permissionLinks.Where(link =>
            link.MenuItemId != item.Id || link.PermissionId != readPermission.Id));
        context.MenuItemRoles.RemoveRange(roleLinks);

        if (!permissionLinks.Any(link =>
                link.MenuItemId == item.Id && link.PermissionId == readPermission.Id))
        {
            context.MenuItemPermissions.Add(new MenuItemPermission
            {
                MenuItemId = item.Id,
                PermissionId = readPermission.Id
            });
        }

        // The parent must be visible to a user whose only administrative permission is
        // OperationalRestrictions.Read; menu permission checks are OR within a menu item.
        if (!await context.MenuItemPermissions.AnyAsync(link =>
                link.MenuItemId == catalogs.Id && link.PermissionId == readPermission.Id))
        {
            context.MenuItemPermissions.Add(new MenuItemPermission
            {
                MenuItemId = catalogs.Id,
                PermissionId = readPermission.Id
            });
        }

        await context.SaveChangesAsync();
    }
}
