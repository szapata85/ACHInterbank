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
/// Reconciles only the Crear transacción MenuItem so its permission alternatives mirror
/// P0.TransactionsCreate: Transactions.Create OR the legacy CanManageAch compatibility claim.
/// The legacy CanReadAch link is removed because read permission alone cannot create a transaction.
/// </summary>
[Scoped]
public sealed class TransactionsCreateMenuAuthorizationSeeder(AchDbContext context) : IDbSeeder
{
    public const int AuthorizationReconciliationOrder = 102;

    public int Order => AuthorizationReconciliationOrder;

    public async Task SeedAsync()
    {
        var menuItem = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Id == MenuItemConfiguration.TransactionsCreateId);

        if (menuItem is null
            || !string.Equals(menuItem.Route, "/transactions/create", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Canonical Crear transacción MenuItem must exist before its authorization is reconciled.");
        }

        var finePermission = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == TransactionsCreatePermissionSeeder.PermissionId);
        if (finePermission is null
            || !string.Equals(finePermission.Name, FineGrainedPermissions.Transactions.Create, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Transactions.Create must be seeded before Crear transacción menu authorization is reconciled.");
        }

        var managePermission = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == PermissionConfiguration.ManageAchPermissionId);
        if (managePermission is null
            || !string.Equals(managePermission.Name, "CanManageAch", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("CanManageAch must exist before Crear transacción menu authorization is reconciled.");
        }

        var allowedPermissionIds = new HashSet<Guid>
        {
            TransactionsCreatePermissionSeeder.PermissionId,
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
