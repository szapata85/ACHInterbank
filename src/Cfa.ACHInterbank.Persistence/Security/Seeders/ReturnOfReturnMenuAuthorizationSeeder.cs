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
/// NAV3.4K treats Devolución de una devolución as a mixed surface. Entry/evaluation uses the
/// existing P0.ReturnsRead semantics (Returns.Read OR CanReadAch), while both file-generation
/// actions remain separately protected by P0.ReturnsGenerateFile. No role or user grants are created.
/// </summary>
[Scoped]
public sealed class ReturnOfReturnMenuAuthorizationSeeder(AchDbContext context) : IDbSeeder
{
    public const int AuthorizationReconciliationOrder = 111;

    public int Order => AuthorizationReconciliationOrder;

    public async Task SeedAsync()
    {
        var fineRead = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == ReturnsReadPermissionSeeder.ReadPermissionId);
        if (fineRead is null
            || !string.Equals(fineRead.Name, FineGrainedPermissions.Returns.Read, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Returns.Read must exist before Devolución de una devolución authorization is reconciled.");
        }

        var legacyRead = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == PermissionConfiguration.ReadAchPermissionId);
        if (legacyRead is null || !string.Equals(legacyRead.Name, "CanReadAch", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("CanReadAch must exist before Devolución de una devolución authorization is reconciled.");
        }

        var parent = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == NavigationInformationArchitectureSeeder.OperationRoute);
        var menuItem = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == "/transactions/returns-ror");

        if (parent is null
            || !string.Equals(parent.Label, NavigationInformationArchitectureSeeder.OperationLabel, StringComparison.Ordinal)
            || menuItem is null
            || menuItem.ParentId != parent.Id
            || !string.Equals(menuItem.Label, "Devolución de una devolución", StringComparison.Ordinal)
            || menuItem.Order != 70
            || !menuItem.IsActive)
        {
            throw new InvalidOperationException(
                "Canonical Devolución de una devolución MenuItem must exist under Operación at Order 70 before NAV3.4K authorization reconciliation.");
        }

        var allowedPermissionIds = new[]
        {
            ReturnsReadPermissionSeeder.ReadPermissionId,
            PermissionConfiguration.ReadAchPermissionId
        };

        var currentPermissions = await context.MenuItemPermissions
            .Where(link => link.MenuItemId == menuItem.Id)
            .ToListAsync();
        context.MenuItemPermissions.RemoveRange(
            currentPermissions.Where(link => !allowedPermissionIds.Contains(link.PermissionId)));

        foreach (var permissionId in allowedPermissionIds)
        {
            if (!currentPermissions.Any(link => link.PermissionId == permissionId))
            {
                context.MenuItemPermissions.Add(new MenuItemPermission
                {
                    MenuItemId = menuItem.Id,
                    PermissionId = permissionId
                });
            }
        }

        var roleLinks = await context.MenuItemRoles
            .Where(link => link.MenuItemId == menuItem.Id)
            .ToListAsync();
        context.MenuItemRoles.RemoveRange(roleLinks);

        await context.SaveChangesAsync();
    }
}
