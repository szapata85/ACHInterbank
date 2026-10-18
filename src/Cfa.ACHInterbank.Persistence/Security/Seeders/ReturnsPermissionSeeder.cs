using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Persists the already-defined fine-grained Returns.GenerateFile permission so the SPA
/// can use the same mutable capability accepted by P0.ReturnsGenerateFile.
/// This seeder does not grant the permission to any role or user.
/// </summary>
[Scoped]
public sealed class ReturnsPermissionSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid GenerateFilePermissionId = Guid.Parse("826efd89-3c7e-4237-92b6-ad5b8d7f53ac");

    public int Order => 0;

    public async Task SeedAsync()
    {
        var permission = await context.Permissions
            .FirstOrDefaultAsync(item => item.Name == FineGrainedPermissions.Returns.GenerateFile)
            ?? await context.Permissions.FindAsync(GenerateFilePermissionId);

        if (permission is null)
        {
            permission = new Permission { Id = GenerateFilePermissionId };
            context.Permissions.Add(permission);
        }

        permission.Name = FineGrainedPermissions.Returns.GenerateFile;
        permission.Description = "Generar archivos de devoluciones ACH";

        await context.SaveChangesAsync();
    }
}
