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
/// Reconciles only Cargar transacciones desde archivo so its direct permission alternatives
/// mirror P1.BulkIngestionUpload: BulkIngestion.Upload OR legacy CanManageAch.
/// The existing Admin/ACH.Operator role links are preserved because the shared /transactions
/// parent still has that legacy role prerequisite; NAV3.4L does not refactor the parent.
/// </summary>
[Scoped]
public sealed class BulkIngestionUploadMenuAuthorizationSeeder(AchDbContext context) : IDbSeeder
{
    public const int AuthorizationReconciliationOrder = 112;

    public int Order => AuthorizationReconciliationOrder;

    public async Task SeedAsync()
    {
        var menuItem = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == "/transactions/bulk-ingestion/upload");

        if (menuItem is null)
        {
            throw new InvalidOperationException("Canonical Cargar transacciones desde archivo MenuItem must exist before its authorization is reconciled.");
        }

        var fineUpload = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == BulkIngestionUploadPermissionSeeder.UploadPermissionId);
        if (fineUpload is null
            || !string.Equals(fineUpload.Name, FineGrainedPermissions.BulkIngestion.Upload, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("BulkIngestion.Upload must be seeded before Cargar transacciones desde archivo authorization is reconciled.");
        }

        var legacyManage = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == PermissionConfiguration.ManageAchPermissionId);
        if (legacyManage is null
            || !string.Equals(legacyManage.Name, "CanManageAch", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("CanManageAch must exist before Cargar transacciones desde archivo authorization is reconciled.");
        }

        var allowedPermissionIds = new HashSet<Guid>
        {
            BulkIngestionUploadPermissionSeeder.UploadPermissionId,
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
