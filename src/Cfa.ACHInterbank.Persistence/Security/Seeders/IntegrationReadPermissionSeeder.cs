using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Materializes the pre-existing Integrations.Read capability for NAV3.4R. No role or user
/// grant is created; CanReadAch remains the compatibility fallback in P1.IntegrationsRead.
/// </summary>
[Scoped]
public sealed class IntegrationReadPermissionSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid ReadPermissionId = Guid.Parse("e6cfca32-17a9-45c0-a6f9-2a023317fbce");

    public int Order => 0;

    public async Task SeedAsync()
    {
        var permission = await context.Permissions
            .FirstOrDefaultAsync(item => item.Name == FineGrainedPermissions.Integrations.Read)
            ?? await context.Permissions.FindAsync(ReadPermissionId);

        if (permission is null)
        {
            permission = new Permission { Id = ReadPermissionId };
            context.Permissions.Add(permission);
        }

        permission.Name = FineGrainedPermissions.Integrations.Read;
        permission.Description = "Consultar configuración de integraciones";

        await context.SaveChangesAsync();
    }
}
