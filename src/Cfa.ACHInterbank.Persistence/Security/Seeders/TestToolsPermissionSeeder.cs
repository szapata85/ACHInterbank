using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Persists the dedicated test-tools permission without granting it to functional roles.
/// NAV3.1 establishes only the permission foundation; role provisioning and enforcement
/// remain explicit follow-up decisions.
/// </summary>
[Scoped]
public sealed class TestToolsPermissionSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid AccessPermissionId = Guid.Parse("820d447b-2c29-4255-9627-a47b0e8ee641");

    public int Order => 0;

    public async Task SeedAsync()
    {
        var permission = await context.Permissions
            .FirstOrDefaultAsync(item => item.Name == FineGrainedPermissions.TestTools.Access)
            ?? await context.Permissions.FindAsync(AccessPermissionId);

        if (permission is null)
        {
            permission = new Permission { Id = AccessPermissionId };
            context.Permissions.Add(permission);
        }

        permission.Name = FineGrainedPermissions.TestTools.Access;
        permission.Description = "Acceso a herramientas de prueba";

        await context.SaveChangesAsync();
    }
}
