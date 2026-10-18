using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Reconciles only Seguimiento de cargas so its direct permission alternatives mirror
/// P1.BulkIngestionRead: BulkIngestion.Read OR legacy CanReadAch. Retry and cancellation
/// remain separately protected by their action policies and SPA fail-closed gates.
/// </summary>
[Scoped]
public sealed class BulkIngestionTrackingMenuAuthorizationSeeder(AchDbContext context) : IDbSeeder
{
    public const int AuthorizationReconciliationOrder = 105;

    public int Order => AuthorizationReconciliationOrder;

    public async Task SeedAsync()
    {
        var menuItem = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == "/transactions/bulk-ingestion/tracking");

        if (menuItem is null)
        {
            throw new InvalidOperationException("Canonical Seguimiento de cargas MenuItem must exist before its authorization is reconciled.");
        }

        var fineRead = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == BulkIngestionTrackingPermissionSeeder.ReadPermissionId);
        if (fineRead is null
            || !string.Equals(fineRead.Name, FineGrainedPermissions.BulkIngestion.Read, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("BulkIngestion.Read must be seeded before Seguimiento de cargas menu authorization is reconciled.");
        }

        var legacyRead = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == PermissionConfiguration.ReadAchPermissionId);
        if (legacyRead is null
            || !string.Equals(legacyRead.Name, "CanReadAch", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("CanReadAch must exist before Seguimiento de cargas menu authorization is reconciled.");
        }

        var allowedPermissionIds = new HashSet<Guid>
        {
            BulkIngestionTrackingPermissionSeeder.ReadPermissionId,
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
