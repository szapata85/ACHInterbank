using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// NAV3.4N makes Conexiones a structural menu container without a role gate.
/// Its permission alternatives are the union already required by its current children:
/// CanManageUsers for the three legacy integration children, plus Config.Read/CanReadAch
/// for Configuración del intercambio de archivos. Child authorization remains independent.
/// </summary>
[Scoped]
public sealed class ConnectionsStructuralMenuAuthorizationSeeder(AchDbContext context) : IDbSeeder
{
    public const int AuthorizationReconciliationOrder = 114;
    public const string ConnectionsRoute = "/integraciones";

    public int Order => AuthorizationReconciliationOrder;

    public async Task SeedAsync()
    {
        var connections = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == ConnectionsRoute)
            ?? throw new InvalidOperationException(
                "Canonical Conexiones MenuItem must exist before structural authorization is reconciled.");

        if (connections.Id != MenuItemConfiguration.IntegrationsId)
        {
            throw new InvalidOperationException(
                $"Canonical Conexiones MenuItem must keep ID {MenuItemConfiguration.IntegrationsId}.");
        }

        await EnsurePermissionAsync(
            PermissionConfiguration.ManageUsersPermissionId,
            "CanManageUsers");
        await EnsurePermissionAsync(
            ConfigurationPermissionSeeder.ReadPermissionId,
            FineGrainedPermissions.Config.Read);
        await EnsurePermissionAsync(
            PermissionConfiguration.ReadAchPermissionId,
            "CanReadAch");

        var allowedParentPermissionIds = new HashSet<Guid>
        {
            PermissionConfiguration.ManageUsersPermissionId,
            ConfigurationPermissionSeeder.ReadPermissionId,
            PermissionConfiguration.ReadAchPermissionId
        };

        var parentPermissionLinks = await context.MenuItemPermissions
            .Where(link => link.MenuItemId == connections.Id)
            .ToListAsync();

        context.MenuItemPermissions.RemoveRange(
            parentPermissionLinks.Where(link => !allowedParentPermissionIds.Contains(link.PermissionId)));

        foreach (var permissionId in allowedParentPermissionIds)
        {
            if (!parentPermissionLinks.Any(link => link.PermissionId == permissionId))
            {
                context.MenuItemPermissions.Add(new MenuItemPermission
                {
                    MenuItemId = connections.Id,
                    PermissionId = permissionId
                });
            }
        }

        var parentRoleLinks = await context.MenuItemRoles
            .Where(link => link.MenuItemId == connections.Id)
            .ToListAsync();
        context.MenuItemRoles.RemoveRange(parentRoleLinks);

        var mft = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == AchColombiaMftAdministrationMenuSeeder.Route)
            ?? throw new InvalidOperationException(
                "Canonical Configuración del intercambio de archivos MenuItem must exist before Conexiones is finalized.");

        var mftRoleLinks = await context.MenuItemRoles
            .Where(link => link.MenuItemId == mft.Id)
            .ToListAsync();
        context.MenuItemRoles.RemoveRange(mftRoleLinks);

        await context.SaveChangesAsync();
    }

    private async Task EnsurePermissionAsync(Guid id, string expectedName)
    {
        var permission = await context.Permissions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id);
        if (permission is null || !string.Equals(permission.Name, expectedName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Permission '{expectedName}' must exist before Conexiones structural authorization is reconciled.");
        }
    }
}
