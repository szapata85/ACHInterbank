using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// NAV3.4J treats Sobre digital as a mixed surface: CanReadAch is the effective entry/preparation
/// requirement because the page loads the operational clearing-house context, while encrypt/decrypt
/// are authorized independently by their fine-grained backend policies. The MenuItem therefore
/// represents only the usable entry boundary and creates no role or user grants.
/// </summary>
[Scoped]
public sealed class DigitalEnvelopeMenuAuthorizationSeeder(AchDbContext context) : IDbSeeder
{
    public const int AuthorizationReconciliationOrder = 110;

    public int Order => AuthorizationReconciliationOrder;

    public async Task SeedAsync()
    {
        var legacyRead = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == PermissionConfiguration.ReadAchPermissionId);
        if (legacyRead is null || !string.Equals(legacyRead.Name, "CanReadAch", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("CanReadAch must exist before Cifrar y descifrar archivos authorization is reconciled.");
        }

        var parent = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Id == MenuItemConfiguration.NachaSecurityId);
        var menuItem = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Id == MenuItemConfiguration.DigitalEnvelopeId);

        if (parent is null
            || !string.Equals(parent.Route, "/nacha-security", StringComparison.Ordinal)
            || menuItem is null
            || menuItem.ParentId != parent.Id
            || !string.Equals(menuItem.Label, "Cifrar y descifrar archivos", StringComparison.Ordinal)
            || !string.Equals(menuItem.Route, "/nacha-security/sobre-digital", StringComparison.Ordinal)
            || menuItem.Order != 20
            || !menuItem.IsActive)
        {
            throw new InvalidOperationException(
                "Canonical Cifrar y descifrar archivos MenuItem must exist under Seguridad de archivos at Order 20 before NAV3.4J authorization reconciliation.");
        }

        var currentPermissions = await context.MenuItemPermissions
            .Where(link => link.MenuItemId == menuItem.Id)
            .ToListAsync();
        context.MenuItemPermissions.RemoveRange(
            currentPermissions.Where(link => link.PermissionId != PermissionConfiguration.ReadAchPermissionId));

        if (!currentPermissions.Any(link => link.PermissionId == PermissionConfiguration.ReadAchPermissionId))
        {
            context.MenuItemPermissions.Add(new MenuItemPermission
            {
                MenuItemId = menuItem.Id,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            });
        }

        var roleLinks = await context.MenuItemRoles
            .Where(link => link.MenuItemId == menuItem.Id)
            .ToListAsync();
        context.MenuItemRoles.RemoveRange(roleLinks);

        await context.SaveChangesAsync();
    }
}
