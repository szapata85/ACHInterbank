using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Persists the NAV3.4Q connection-management capability. No role or user grant is created;
/// CanManageAch remains the compatibility fallback in P1.IntegrationConnectionsManage.
/// </summary>
[Scoped]
public sealed class IntegrationConnectionPermissionSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid ManageConnectionsPermissionId = Guid.Parse("289e92ea-abd4-43a9-b93a-23b06fb03362");

    public int Order => 0;

    public async Task SeedAsync()
    {
        var permission = await context.Permissions
            .FirstOrDefaultAsync(item => item.Name == FineGrainedPermissions.Integrations.ManageConnections)
            ?? await context.Permissions.FindAsync(ManageConnectionsPermissionId);

        if (permission is null)
        {
            permission = new Permission { Id = ManageConnectionsPermissionId };
            context.Permissions.Add(permission);
        }

        permission.Name = FineGrainedPermissions.Integrations.ManageConnections;
        permission.Description = "Administrar conexiones de integración";

        await context.SaveChangesAsync();
    }
}
