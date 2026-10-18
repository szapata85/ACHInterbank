using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Grants the canonical test-tools access permission to the existing highest-privilege
/// administrative role. NAV3.2A does not create roles and does not change tool enforcement.
/// </summary>
[Scoped]
public sealed class TestToolsAdminRolePermissionSeeder(AchDbContext context) : IDbSeeder
{
    public int Order => 1;

    public async Task SeedAsync()
    {
        var adminRole = await context.Roles
            .AsNoTracking()
            .SingleOrDefaultAsync(role => role.Id == RoleConfiguration.AdminRoleId);

        if (adminRole is null || !string.Equals(adminRole.Name, "Admin", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The seeded Admin role identity is missing or inconsistent.");
        }

        var permission = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == TestToolsPermissionSeeder.AccessPermissionId);

        if (permission is null || !string.Equals(permission.Name, FineGrainedPermissions.TestTools.Access, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("TestTools.Access must be seeded before its Admin role grant.");
        }

        if (!await context.RolePermissions.AnyAsync(item =>
                item.RoleId == RoleConfiguration.AdminRoleId
                && item.PermissionId == TestToolsPermissionSeeder.AccessPermissionId))
        {
            context.RolePermissions.Add(new RolePermission
            {
                RoleId = RoleConfiguration.AdminRoleId,
                PermissionId = TestToolsPermissionSeeder.AccessPermissionId
            });
            await context.SaveChangesAsync();
        }
    }
}
