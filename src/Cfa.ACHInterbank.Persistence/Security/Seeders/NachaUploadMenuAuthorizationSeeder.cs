using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Reconciles only the Cargar archivo NACHA-M MenuItem so its permission alternatives mirror
/// P1.NachaUpload: Nacha.Upload OR the legacy CanManageAch compatibility claim.
/// Read-only permissions are not valid alternatives for upload/ingestion.
/// </summary>
[Scoped]
public sealed class NachaUploadMenuAuthorizationSeeder(AchDbContext context) : IDbSeeder
{
    public const int AuthorizationReconciliationOrder = 103;

    public int Order => AuthorizationReconciliationOrder;

    public async Task SeedAsync()
    {
        var menuItem = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Id == MenuItemConfiguration.NachaUploadId);

        if (menuItem is null
            || !string.Equals(menuItem.Route, "/transactions/nacha-upload", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Canonical Cargar archivo NACHA-M MenuItem must exist before its authorization is reconciled.");
        }

        var finePermission = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == NachaUploadPermissionSeeder.PermissionId);
        if (finePermission is null
            || !string.Equals(finePermission.Name, FineGrainedPermissions.Nacha.Upload, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Nacha.Upload must be seeded before Cargar archivo NACHA-M menu authorization is reconciled.");
        }

        var managePermission = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == PermissionConfiguration.ManageAchPermissionId);
        if (managePermission is null
            || !string.Equals(managePermission.Name, "CanManageAch", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("CanManageAch must exist before Cargar archivo NACHA-M menu authorization is reconciled.");
        }

        var allowedPermissionIds = new HashSet<Guid>
        {
            NachaUploadPermissionSeeder.PermissionId,
            PermissionConfiguration.ManageAchPermissionId
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
