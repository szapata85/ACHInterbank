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
/// Makes TestTools.Access the sole menu permission for the canonical test-tools domain.
/// NAV3.2B intentionally removes legacy MenuItemRole and generic ACH permission links so
/// menu visibility is governed by the dedicated permission rather than role names or fallbacks.
/// </summary>
[Scoped]
public sealed class TestToolsMenuAuthorizationSeeder(AchDbContext context) : IDbSeeder
{
    public const int AuthorizationReconciliationOrder = 101;

    public int Order => AuthorizationReconciliationOrder;

    public async Task SeedAsync()
    {
        var permission = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == TestToolsPermissionSeeder.AccessPermissionId);

        if (permission is null || !string.Equals(permission.Name, FineGrainedPermissions.TestTools.Access, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("TestTools.Access must be seeded before test-tools menu authorization is reconciled.");
        }

        var testTools = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Id == MenuItemConfiguration.UatSimulatorsId);
        var inboundSimulator = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Id == MenuItemConfiguration.NachaInboundSimulatorId);
        var serviceTests = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == NavigationInformationArchitectureSeeder.ServiceTestsRoute);

        if (testTools is null
            || inboundSimulator is null
            || serviceTests is null
            || !string.Equals(testTools.Route, NavigationInformationArchitectureSeeder.TestToolsRoute, StringComparison.Ordinal)
            || !string.Equals(inboundSimulator.Route, NavigationInformationArchitectureSeeder.InboundFileSimulatorRoute, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Canonical test-tools MenuItems must exist before their authorization is reconciled.");
        }

        await RequireTestToolsAccessOnlyAsync(testTools.Id);
        await RequireTestToolsAccessOnlyAsync(inboundSimulator.Id);
        await RequireTestToolsAccessOnlyAsync(serviceTests.Id);

        // NAV3.3B activates Pruebas de servicios only after its SPA and API boundaries are
        // independently enforced by TestTools.Access without environment gating.
        serviceTests.IsActive = true;

        await context.SaveChangesAsync();
    }

    private async Task RequireTestToolsAccessOnlyAsync(int menuItemId)
    {
        var permissionLinks = await context.MenuItemPermissions
            .Where(link => link.MenuItemId == menuItemId)
            .ToListAsync();

        context.MenuItemPermissions.RemoveRange(
            permissionLinks.Where(link => link.PermissionId != TestToolsPermissionSeeder.AccessPermissionId));

        if (!permissionLinks.Any(link => link.PermissionId == TestToolsPermissionSeeder.AccessPermissionId))
        {
            context.MenuItemPermissions.Add(new MenuItemPermission
            {
                MenuItemId = menuItemId,
                PermissionId = TestToolsPermissionSeeder.AccessPermissionId
            });
        }

        var roleLinks = await context.MenuItemRoles
            .Where(link => link.MenuItemId == menuItemId)
            .ToListAsync();
        context.MenuItemRoles.RemoveRange(roleLinks);
    }
}
