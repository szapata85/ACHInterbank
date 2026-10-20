using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Persists the already-defined fine-grained capabilities used by the managed digital-envelope
/// encrypt/decrypt endpoints. NAV3.4J deliberately creates no role or direct user grants;
/// CanManageAch remains the temporary backend fallback for each cryptographic operation.
/// </summary>
[Scoped]
public sealed class DigitalEnvelopeOperationPermissionSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid EncryptPermissionId = Guid.Parse("d951161d-06c5-4e31-a574-188e5094f59d");
    public static readonly Guid DecryptPermissionId = Guid.Parse("7ccf7a90-d66f-4e63-bcb9-92275118c7e1");

    public int Order => 0;

    public async Task SeedAsync()
    {
        await EnsurePermissionAsync(
            EncryptPermissionId,
            FineGrainedPermissions.DigitalEnvelope.Encrypt,
            "Cifrar archivos mediante sobre digital");
        await EnsurePermissionAsync(
            DecryptPermissionId,
            FineGrainedPermissions.DigitalEnvelope.Decrypt,
            "Descifrar archivos mediante sobre digital");
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
