using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Persists the already-defined fine-grained permission used by P1.BulkIngestionUpload.
/// NAV3.4L does not grant this permission to any role or user; CanManageAch remains the
/// temporary backend compatibility fallback until grants are migrated explicitly.
/// </summary>
[Scoped]
public sealed class BulkIngestionUploadPermissionSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid UploadPermissionId = Guid.Parse("e8194742-09ca-4148-9567-75bb24923f94");

    public int Order => 0;

    public async Task SeedAsync()
    {
        var permission = await context.Permissions
            .FirstOrDefaultAsync(item => item.Name == FineGrainedPermissions.BulkIngestion.Upload)
            ?? await context.Permissions.FindAsync(UploadPermissionId);

        if (permission is null)
        {
            permission = new Permission { Id = UploadPermissionId };
            context.Permissions.Add(permission);
        }

        permission.Name = FineGrainedPermissions.BulkIngestion.Upload;
        permission.Description = "Cargar transacciones desde archivo";

        await context.SaveChangesAsync();
    }
}
