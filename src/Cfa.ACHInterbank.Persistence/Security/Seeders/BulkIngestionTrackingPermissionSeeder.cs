using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Persists the already-defined fine-grained permissions used by the bulk-ingestion
/// tracking/read, retry, and cancel policies. This seeder does not grant permissions
/// to any role or user and does not materialize the separate upload capability.
/// </summary>
[Scoped]
public sealed class BulkIngestionTrackingPermissionSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid ReadPermissionId = Guid.Parse("4902ced3-1144-432c-afc3-4d8f945c2ca8");
    public static readonly Guid RetryPermissionId = Guid.Parse("a2584f2a-ab66-4d30-b7f8-c69ca057d79e");
    public static readonly Guid CancelPermissionId = Guid.Parse("fd20a8b5-739b-4384-8cce-4189bd5af106");

    public int Order => 0;

    public async Task SeedAsync()
    {
        await EnsurePermissionAsync(ReadPermissionId, FineGrainedPermissions.BulkIngestion.Read, "Consultar seguimiento de cargas masivas");
        await EnsurePermissionAsync(RetryPermissionId, FineGrainedPermissions.BulkIngestion.Retry, "Reintentar procesamiento de cargas masivas");
        await EnsurePermissionAsync(CancelPermissionId, FineGrainedPermissions.BulkIngestion.Cancel, "Cancelar procesamiento de cargas masivas");
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
