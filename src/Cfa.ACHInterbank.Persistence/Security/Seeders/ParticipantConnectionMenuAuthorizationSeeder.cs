using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Reconciles only Conexión entidad participante. Entry/read remains legacy CanReadAch because
/// the parent /integraciones and the integration read boundary still rely on that capability.
/// The historical Admin MenuItemRole is deliberately preserved so NAV3.4Q does not widen
/// menu discoverability while introducing connection-specific management without grants.
/// </summary>
[Scoped]
public sealed class ParticipantConnectionMenuAuthorizationSeeder(AchDbContext context) : IDbSeeder
{
    public const int AuthorizationReconciliationOrder = 117;
    public const string Route = "/integraciones/conexion";

    public int Order => AuthorizationReconciliationOrder;

    public async Task SeedAsync()
    {
        var menuItem = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == Route)
            ?? throw new InvalidOperationException(
                "Canonical Conexión entidad participante MenuItem must exist before its authorization is reconciled.");

        var legacyRead = await context.Permissions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == PermissionConfiguration.ReadAchPermissionId);
        if (legacyRead is null || !string.Equals(legacyRead.Name, "CanReadAch", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "CanReadAch must exist before Conexión entidad participante authorization is reconciled.");
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
