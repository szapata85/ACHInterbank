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
/// Reconciles only Configuración del intercambio de archivos so its direct permission
/// alternatives mirror P1.ConfigRead: Config.Read OR legacy CanReadAch.
///
/// The existing direct MenuItemRole is intentionally preserved in NAV3.4M. The canonical
/// parent /integraciones still has a legacy Admin + CanManageUsers gate and the Angular
/// NavigationService still flattens that navigable parent. Removing only this child's role
/// would promote it to a root for principals filtered out by the parent. Parent/navigation
/// normalization is a separate prerequisite and is not expanded into this increment.
/// </summary>
[Scoped]
public sealed class MftAdministrationMenuAuthorizationSeeder(AchDbContext context) : IDbSeeder
{
    public const int AuthorizationReconciliationOrder = 113;

    public int Order => AuthorizationReconciliationOrder;

    public async Task SeedAsync()
    {
        var menuItem = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == AchColombiaMftAdministrationMenuSeeder.Route);
        if (menuItem is null)
        {
            throw new InvalidOperationException(
                "Canonical Configuración del intercambio de archivos MenuItem must exist before its authorization is reconciled.");
        }

        var fineRead = await context.Permissions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == ConfigurationPermissionSeeder.ReadPermissionId);
        if (fineRead is null
            || !string.Equals(fineRead.Name, FineGrainedPermissions.Config.Read, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Config.Read must be seeded before MFT administration menu authorization is reconciled.");
        }

        var legacyRead = await context.Permissions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == PermissionConfiguration.ReadAchPermissionId);
        if (legacyRead is null || !string.Equals(legacyRead.Name, "CanReadAch", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "CanReadAch must exist before MFT administration menu authorization is reconciled.");
        }

        var allowedPermissionIds = new HashSet<Guid>
        {
            ConfigurationPermissionSeeder.ReadPermissionId,
            PermissionConfiguration.ReadAchPermissionId
        };

        var permissionLinks = await context.MenuItemPermissions
            .Where(link => link.MenuItemId == menuItem.Id)
            .ToListAsync();

        context.MenuItemPermissions.RemoveRange(
            permissionLinks.Where(link => !allowedPermissionIds.Contains(link.PermissionId)));

        foreach (var permissionId in allowedPermissionIds)
        {
            if (!permissionLinks.Any(link => link.PermissionId == permissionId))
            {
                context.MenuItemPermissions.Add(new MenuItemPermission
                {
                    MenuItemId = menuItem.Id,
                    PermissionId = permissionId
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
