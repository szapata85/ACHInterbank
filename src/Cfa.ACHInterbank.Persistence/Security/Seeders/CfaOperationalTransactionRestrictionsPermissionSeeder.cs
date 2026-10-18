using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

[Scoped]
public sealed class CfaOperationalTransactionRestrictionsPermissionSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid ReadPermissionId = Guid.Parse("cfa00015-0a11-4f00-a100-000000000001");
    public static readonly Guid ManagePermissionId = Guid.Parse("cfa00015-0a11-4f00-a100-000000000002");

    public int Order => 0;

    public async Task SeedAsync()
    {
        var read = await UpsertPermissionAsync(
            ReadPermissionId,
            FineGrainedPermissions.OperationalRestrictions.Read,
            "Consultar restricciones operacionales CFA");
        var manage = await UpsertPermissionAsync(
            ManagePermissionId,
            FineGrainedPermissions.OperationalRestrictions.Manage,
            "Administrar restricciones operacionales CFA");
        await context.SaveChangesAsync();

        await EnsureRolePermissionAsync(RoleConfiguration.AdminRoleId, read.Id);
        await EnsureRolePermissionAsync(RoleConfiguration.OperatorRoleId, read.Id);
        await EnsureRolePermissionAsync(RoleConfiguration.AdminRoleId, manage.Id);
        await context.SaveChangesAsync();
    }

    private async Task<Permission> UpsertPermissionAsync(Guid id, string name, string description)
    {
        var permission = await context.Permissions.FirstOrDefaultAsync(x => x.Name == name)
            ?? await context.Permissions.FindAsync(id);

        if (permission is null)
        {
            permission = new Permission { Id = id };
            context.Permissions.Add(permission);
        }

        permission.Name = name;
        permission.Description = description;
        return permission;
    }

    private async Task EnsureRolePermissionAsync(Guid roleId, Guid permissionId)
    {
        if (!await context.RolePermissions.AnyAsync(x => x.RoleId == roleId && x.PermissionId == permissionId))
        {
            context.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });
        }
    }
}
