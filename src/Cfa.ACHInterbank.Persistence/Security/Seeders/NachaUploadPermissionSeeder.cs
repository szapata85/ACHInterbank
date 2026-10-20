using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Persists the already-defined fine-grained Nacha.Upload permission so navigation
/// can reference the same upload capability accepted by P1.NachaUpload.
/// This seeder does not grant the permission to any role or user.
/// </summary>
[Scoped]
public sealed class NachaUploadPermissionSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid PermissionId = Guid.Parse("9d97cb63-8bbe-4253-970c-a490f3c80e10");

    public int Order => 0;

    public async Task SeedAsync()
    {
        var permission = await context.Permissions
            .FirstOrDefaultAsync(item => item.Name == FineGrainedPermissions.Nacha.Upload)
            ?? await context.Permissions.FindAsync(PermissionId);

        if (permission is null)
        {
            permission = new Permission { Id = PermissionId };
            context.Permissions.Add(permission);
        }

        permission.Name = FineGrainedPermissions.Nacha.Upload;
        permission.Description = "Cargar e ingerir archivos NACHA-M";

        await context.SaveChangesAsync();
    }
}
