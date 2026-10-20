using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// NAV3.4R reconciles only the read/discoverability model of the Conexiones subtree.
/// The three /integraciones children remain role-scoped to Admin OR ACH.Operator and use
/// Integrations.Read OR CanReadAch. The structural parent remains role-free and carries the
/// union needed by integration reads and MFT reads. No grants are created.
/// </summary>
[Scoped]
public sealed class IntegrationReadNavigationAudienceSeeder(AchDbContext context) : IDbSeeder
{
    public const int AuthorizationReconciliationOrder = 118;

    private static readonly string[] IntegrationChildRoutes =
    [
        IntegrationContractMenuAuthorizationSeeder.Route,
        InformationRelationshipMenuAuthorizationSeeder.Route,
        ParticipantConnectionMenuAuthorizationSeeder.Route
    ];

    public int Order => AuthorizationReconciliationOrder;

    public async Task SeedAsync()
    {
        await EnsurePermissionAsync(IntegrationReadPermissionSeeder.ReadPermissionId, FineGrainedPermissions.Integrations.Read);
        await EnsurePermissionAsync(ConfigurationPermissionSeeder.ReadPermissionId, FineGrainedPermissions.Config.Read);
        await EnsurePermissionAsync(PermissionConfiguration.ReadAchPermissionId, "CanReadAch");

        var connections = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == ConnectionsStructuralMenuAuthorizationSeeder.ConnectionsRoute)
            ?? throw new InvalidOperationException("Canonical Conexiones MenuItem must exist before NAV3.4R authorization is reconciled.");

        await ReconcilePermissionsAsync(connections.Id,
        [
            IntegrationReadPermissionSeeder.ReadPermissionId,
            ConfigurationPermissionSeeder.ReadPermissionId,
            PermissionConfiguration.ReadAchPermissionId
        ]);

        var parentRoles = await context.MenuItemRoles
            .Where(link => link.MenuItemId == connections.Id)
            .ToListAsync();
        context.MenuItemRoles.RemoveRange(parentRoles);

        foreach (var route in IntegrationChildRoutes)
        {
            var child = await context.MenuItems
                .SingleOrDefaultAsync(item => item.Route == route)
                ?? throw new InvalidOperationException($"Canonical integration MenuItem '{route}' must exist before NAV3.4R authorization is reconciled.");

            await ReconcilePermissionsAsync(child.Id,
            [
                IntegrationReadPermissionSeeder.ReadPermissionId,
                PermissionConfiguration.ReadAchPermissionId
            ]);

            await ReconcileRolesAsync(child.Id,
            [
                RoleConfiguration.AdminRoleId,
                RoleConfiguration.OperatorRoleId
            ]);
        }

        await context.SaveChangesAsync();
    }

    private async Task ReconcilePermissionsAsync(int menuItemId, IReadOnlyCollection<Guid> allowedPermissionIds)
    {
        var allowed = allowedPermissionIds.ToHashSet();
        var links = await context.MenuItemPermissions
            .Where(link => link.MenuItemId == menuItemId)
            .ToListAsync();

        context.MenuItemPermissions.RemoveRange(links.Where(link => !allowed.Contains(link.PermissionId)));

        foreach (var permissionId in allowed)
        {
            if (!links.Any(link => link.PermissionId == permissionId))
            {
                context.MenuItemPermissions.Add(new MenuItemPermission
                {
                    MenuItemId = menuItemId,
                    PermissionId = permissionId
                });
            }
        }
    }

    private async Task ReconcileRolesAsync(int menuItemId, IReadOnlyCollection<Guid> allowedRoleIds)
    {
        var allowed = allowedRoleIds.ToHashSet();
        var links = await context.MenuItemRoles
            .Where(link => link.MenuItemId == menuItemId)
            .ToListAsync();

        context.MenuItemRoles.RemoveRange(links.Where(link => !allowed.Contains(link.RoleId)));

        foreach (var roleId in allowed)
        {
            if (!links.Any(link => link.RoleId == roleId))
            {
                context.MenuItemRoles.Add(new MenuItemRole
                {
                    MenuItemId = menuItemId,
                    RoleId = roleId
                });
            }
        }
    }

    private async Task EnsurePermissionAsync(Guid id, string expectedName)
    {
        var permission = await context.Permissions.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
        if (permission is null || !string.Equals(permission.Name, expectedName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Permission '{expectedName}' must exist before NAV3.4R authorization is reconciled.");
        }
    }
}
