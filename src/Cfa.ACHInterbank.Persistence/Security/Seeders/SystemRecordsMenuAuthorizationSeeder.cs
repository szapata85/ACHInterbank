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
/// Reconciles Registros del sistema and its three administrative read children.
/// Each child accepts its dedicated semantic read capability OR the legacy capability
/// that preserves today's effective audience. The structural parent accepts the union
/// of all child alternatives and therefore grants no child access by itself.
/// </summary>
[Scoped]
public sealed class SystemRecordsMenuAuthorizationSeeder(AchDbContext context) : IDbSeeder
{
    public const int AuthorizationReconciliationOrder = 107;

    public int Order => AuthorizationReconciliationOrder;

    public async Task SeedAsync()
    {
        await EnsurePermissionAsync(SystemRecordPermissionsSeeder.AuditReadPermissionId, FineGrainedPermissions.AuditLogs.Read);
        await EnsurePermissionAsync(SystemRecordPermissionsSeeder.AuthReadPermissionId, FineGrainedPermissions.AuthLogs.Read);
        await EnsurePermissionAsync(SystemRecordPermissionsSeeder.NavigationReadPermissionId, FineGrainedPermissions.NavigationLogs.Read);
        await EnsurePermissionAsync(PermissionConfiguration.ManageUsersPermissionId, "CanManageUsers");
        await EnsurePermissionAsync(PermissionConfiguration.ReadAchPermissionId, "CanReadAch");

        await ReconcileMenuItemAsync(
            MenuItemConfiguration.AuditLogId,
            NavigationInformationArchitectureSeeder.AuditLogRoute,
            SystemRecordPermissionsSeeder.AuditReadPermissionId,
            PermissionConfiguration.ManageUsersPermissionId);

        await ReconcileMenuItemAsync(
            MenuItemConfiguration.AuthLogId,
            NavigationInformationArchitectureSeeder.AuthenticationLogRoute,
            SystemRecordPermissionsSeeder.AuthReadPermissionId,
            PermissionConfiguration.ManageUsersPermissionId);

        var navigationLog = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == NavigationInformationArchitectureSeeder.NavigationLogRoute);
        if (navigationLog is null)
        {
            throw new InvalidOperationException("Canonical Registro de navegación MenuItem must exist before its authorization is reconciled.");
        }
        await ReconcileMenuItemAsync(
            navigationLog.Id,
            NavigationInformationArchitectureSeeder.NavigationLogRoute,
            SystemRecordPermissionsSeeder.NavigationReadPermissionId,
            PermissionConfiguration.ReadAchPermissionId);

        await ReconcileMenuItemAsync(
            MenuItemConfiguration.LogsId,
            NavigationInformationArchitectureSeeder.SystemRecordsRoute,
            SystemRecordPermissionsSeeder.AuditReadPermissionId,
            SystemRecordPermissionsSeeder.AuthReadPermissionId,
            SystemRecordPermissionsSeeder.NavigationReadPermissionId,
            PermissionConfiguration.ManageUsersPermissionId,
            PermissionConfiguration.ReadAchPermissionId);

        await context.SaveChangesAsync();
    }

    private async Task EnsurePermissionAsync(Guid id, string name)
    {
        var permission = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id);
        if (permission is null || !string.Equals(permission.Name, name, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{name} must exist before Registros del sistema authorization is reconciled.");
        }
    }

    private async Task ReconcileMenuItemAsync(int menuItemId, string expectedRoute, params Guid[] allowedPermissionIds)
    {
        var menuItem = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Id == menuItemId);
        if (menuItem is null || !string.Equals(menuItem.Route, expectedRoute, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Canonical MenuItem '{expectedRoute}' must exist before its authorization is reconciled.");
        }

        var allowed = allowedPermissionIds.ToHashSet();
        var permissionLinks = await context.MenuItemPermissions
            .Where(link => link.MenuItemId == menuItem.Id)
            .ToListAsync();

        context.MenuItemPermissions.RemoveRange(
            permissionLinks.Where(link => !allowed.Contains(link.PermissionId)));

        foreach (var permissionId in allowed)
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
    }
}
