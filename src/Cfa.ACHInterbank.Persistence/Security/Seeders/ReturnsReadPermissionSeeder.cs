using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Persists the already-defined Returns.Read capability so the Return of Return mixed surface
/// can represent the same read/evaluation boundary accepted by P0.ReturnsRead. NAV3.4K does
/// not grant this permission to any role or user.
/// </summary>
[Scoped]
public sealed class ReturnsReadPermissionSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid ReadPermissionId = Guid.Parse("fa144b62-95c2-4540-b2f8-110516eeb837");

    public int Order => 0;

    public async Task SeedAsync()
    {
        var permission = await context.Permissions
            .FirstOrDefaultAsync(item => item.Name == FineGrainedPermissions.Returns.Read)
            ?? await context.Permissions.FindAsync(ReadPermissionId);

        if (permission is null)
        {
            permission = new Permission { Id = ReadPermissionId };
            context.Permissions.Add(permission);
        }

        permission.Name = FineGrainedPermissions.Returns.Read;
        permission.Description = "Consultar devoluciones ACH";

        await context.SaveChangesAsync();
    }
}
