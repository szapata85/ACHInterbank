using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Reconciles only Relación de información. Entry/read remains legacy CanReadAch because
/// the surface consumes shared integration catalog and mapping reads protected by that
/// capability. The existing Admin MenuItemRole is deliberately preserved to avoid widening
/// menu discoverability while NAV3.4P introduces fine mutation capabilities without grants.
/// </summary>
[Scoped]
public sealed class InformationRelationshipMenuAuthorizationSeeder(AchDbContext context) : IDbSeeder
{
    public const int AuthorizationReconciliationOrder = 116;
    public const string Route = "/integraciones/relacion-informacion";

    public int Order => AuthorizationReconciliationOrder;

    public async Task SeedAsync()
    {
        var menuItem = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == Route)
            ?? throw new InvalidOperationException(
                "Canonical Relación de información MenuItem must exist before its authorization is reconciled.");

        var legacyRead = await context.Permissions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == PermissionConfiguration.ReadAchPermissionId);
        if (legacyRead is null || !string.Equals(legacyRead.Name, "CanReadAch", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "CanReadAch must exist before Relación de información authorization is reconciled.");
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
