using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

[Scoped]
public sealed class ClaimsPermissionSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid ReadPermissionId = Guid.Parse("a0420002-2f85-4ca5-a100-000000000010");
    public static readonly Guid CreateDev14PermissionId = Guid.Parse("a0420002-2f85-4ca5-a100-000000000011");
    public static readonly Guid ProgressPermissionId = Guid.Parse("a0420002-2f85-4ca5-a100-000000000012");
    public static readonly Guid ResolvePermissionId = Guid.Parse("a0420002-2f85-4ca5-a100-000000000013");
    public static readonly Guid ExpirePermissionId = Guid.Parse("a0420002-2f85-4ca5-a100-000000000014");

    public int Order => 0;

    public async Task SeedAsync()
    {
        await UpsertPermissionAsync(
            ReadPermissionId,
            FineGrainedPermissions.Claims.Read,
            "Consultar reclamaciones ACH");
        await UpsertPermissionAsync(
            CreateDev14PermissionId,
            FineGrainedPermissions.Claims.CreateDev14,
            "Crear reclamaciones DEV14 por débito ACH no consentido");
        await UpsertPermissionAsync(
            ProgressPermissionId,
            FineGrainedPermissions.Claims.Progress,
            "Registrar avances en reclamaciones ACH");
        await UpsertPermissionAsync(
            ResolvePermissionId,
            FineGrainedPermissions.Claims.Resolve,
            "Resolver reclamaciones ACH");
        await UpsertPermissionAsync(
            ExpirePermissionId,
            FineGrainedPermissions.Claims.Expire,
            "Cerrar reclamaciones ACH por vencimiento");

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
}
