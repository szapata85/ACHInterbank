using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Persists the already-defined fine-grained certificate capabilities used by the
/// certificate administration SPA. This seeder creates no role or user grants.
/// </summary>
[Scoped]
public sealed class CertificateManagementPermissionSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid ReadPermissionId = Guid.Parse("b40c402c-d283-4ba9-a9b2-b63aa583b599");
    public static readonly Guid UploadPublicPermissionId = Guid.Parse("22b2d28a-e4fb-49b1-bb1a-5fe6dc517c27");
    public static readonly Guid RegisterPrivatePermissionId = Guid.Parse("469a6f60-df2c-43ac-88ef-d5760d58121b");
    public static readonly Guid RevokePermissionId = Guid.Parse("2cba2029-c98a-4cf2-9e57-6d81851b74fb");

    public int Order => 0;

    public async Task SeedAsync()
    {
        await EnsurePermissionAsync(ReadPermissionId, FineGrainedPermissions.Certificates.Read, "Consultar certificados digitales");
        await EnsurePermissionAsync(UploadPublicPermissionId, FineGrainedPermissions.Certificates.UploadPublic, "Previsualizar y cargar certificados públicos");
        await EnsurePermissionAsync(RegisterPrivatePermissionId, FineGrainedPermissions.Certificates.RegisterPrivate, "Registrar material privado de certificados");
        await EnsurePermissionAsync(RevokePermissionId, FineGrainedPermissions.Certificates.Revoke, "Revocar o eliminar versiones de certificados");
        await context.SaveChangesAsync();
    }

    private async Task EnsurePermissionAsync(Guid id, string name, string description)
    {
        var permission = await context.Permissions
            .FirstOrDefaultAsync(item => item.Name == name)
            ?? await context.Permissions.FindAsync(id);

        if (permission is null)
        {
            permission = new Permission { Id = id };
            context.Permissions.Add(permission);
        }

        permission.Name = name;
        permission.Description = description;
    }
}
