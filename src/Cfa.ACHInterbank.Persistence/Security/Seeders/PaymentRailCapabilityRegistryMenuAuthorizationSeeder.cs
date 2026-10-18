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
/// Aligns Capacidades de la plataforma with the effective read policy used by SPA/API:
/// CanViewPaymentRailCapabilityRegistry OR CanManageAch OR CanReadAch. Legacy role links
/// are removed so the MenuItem does not add an accidental role AND permission boundary.
/// No role or user grant is created by this reconciler.
/// </summary>
[Scoped]
public sealed class PaymentRailCapabilityRegistryMenuAuthorizationSeeder(AchDbContext context) : IDbSeeder
{
    public const int AuthorizationReconciliationOrder = 109;

    public int Order => AuthorizationReconciliationOrder;

    public async Task SeedAsync()
    {
        await EnsurePermissionAsync(
            PaymentRailCapabilityRegistryPermissionSeeder.PermissionId,
            FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry);
        await EnsurePermissionAsync(PermissionConfiguration.ManageAchPermissionId, "CanManageAch");
        await EnsurePermissionAsync(PermissionConfiguration.ReadAchPermissionId, "CanReadAch");

        var administration = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == NavigationInformationArchitectureSeeder.AdministrationRoute);
        var menuItem = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == NavigationInformationArchitectureSeeder.PlatformCapabilitiesRoute);
        if (administration is null
            || menuItem is null
            || menuItem.ParentId != administration.Id
            || !string.Equals(menuItem.Label, NavigationInformationArchitectureSeeder.PlatformCapabilitiesLabel, StringComparison.Ordinal)
            || menuItem.Order != 50)
        {
            throw new InvalidOperationException(
                "Canonical Capacidades de la plataforma MenuItem must exist under Administración at Order 50 before NAV3.4I authorization reconciliation.");
        }

        await ReconcilePermissionsAsync(menuItem.Id,
        [
            PaymentRailCapabilityRegistryPermissionSeeder.PermissionId,
            PermissionConfiguration.ManageAchPermissionId,
            PermissionConfiguration.ReadAchPermissionId
        ]);

        var roleLinks = await context.MenuItemRoles
            .Where(link => link.MenuItemId == menuItem.Id)
            .ToListAsync();
        context.MenuItemRoles.RemoveRange(roleLinks);

        await context.SaveChangesAsync();
    }

    private async Task EnsurePermissionAsync(Guid id, string name)
    {
        var permission = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id);
        if (permission is null || !string.Equals(permission.Name, name, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{name} must exist before Capacidades de la plataforma authorization is reconciled.");
        }
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
