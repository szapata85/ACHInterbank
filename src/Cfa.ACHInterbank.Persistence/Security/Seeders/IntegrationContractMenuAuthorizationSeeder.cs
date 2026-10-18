using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Reconciles only Contrato del servicio. Its entry/read boundary remains legacy CanReadAch
/// because the page also consumes shared integration catalog/mapping reads protected by that
/// capability and NAV3.4O must not change sibling or parent authorization. The existing Admin
/// MenuItemRole is deliberately preserved so menu discoverability is not widened in this increment.
/// </summary>
[Scoped]
public sealed class IntegrationContractMenuAuthorizationSeeder(AchDbContext context) : IDbSeeder
{
    public const int AuthorizationReconciliationOrder = 115;
    public const string Route = "/integraciones/contrato";

    public int Order => AuthorizationReconciliationOrder;

    public async Task SeedAsync()
    {
        var menuItem = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == Route)
            ?? throw new InvalidOperationException(
                "Canonical Contrato del servicio MenuItem must exist before its authorization is reconciled.");

        var legacyRead = await context.Permissions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == PermissionConfiguration.ReadAchPermissionId);
        if (legacyRead is null || !string.Equals(legacyRead.Name, "CanReadAch", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "CanReadAch must exist before Contrato del servicio authorization is reconciled.");
        }

        var permissionLinks = await context.MenuItemPermissions
            .Where(link => link.MenuItemId == menuItem.Id)
            .ToListAsync();

        context.MenuItemPermissions.RemoveRange(
            permissionLinks.Where(link => link.PermissionId != PermissionConfiguration.ReadAchPermissionId));

        if (!permissionLinks.Any(link => link.PermissionId == PermissionConfiguration.ReadAchPermissionId))
        {
            context.MenuItemPermissions.Add(new MenuItemPermission
            {
                MenuItemId = menuItem.Id,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            });
        }

        await context.SaveChangesAsync();
    }
}
