using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Reconciles only the Devoluciones MenuItem as the read entry point of the mixed returns surface.
/// The complete SPA also consumes the cycle and return-reason read APIs, which currently require
/// legacy CanReadAch. Mutable generation remains separately protected by P0.ReturnsGenerateFile
/// and the SPA action gate.
/// </summary>
[Scoped]
public sealed class ReturnsMenuAuthorizationSeeder(AchDbContext context) : IDbSeeder
{
    public const int AuthorizationReconciliationOrder = 104;

    public int Order => AuthorizationReconciliationOrder;

    public async Task SeedAsync()
    {
        var menuItem = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Id == MenuItemConfiguration.TransactionsReturnsId);

        if (menuItem is null
            || !string.Equals(menuItem.Route, "/transactions/returns", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Canonical Devoluciones MenuItem must exist before its authorization is reconciled.");
        }

        var readPermission = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == PermissionConfiguration.ReadAchPermissionId);
        if (readPermission is null
            || !string.Equals(readPermission.Name, "CanReadAch", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("CanReadAch must exist before Devoluciones menu authorization is reconciled.");
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
