using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Persists the NAV3.4O contract-management capability. No role or user grant is created;
/// CanManageAch remains the compatibility fallback in P1.IntegrationContractsManage.
/// </summary>
[Scoped]
public sealed class IntegrationContractPermissionSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid ManageContractsPermissionId = Guid.Parse("40776ed1-238c-4a59-a890-894a1f7d9c8d");

    public int Order => 0;

    public async Task SeedAsync()
    {
        var permission = await context.Permissions
            .FirstOrDefaultAsync(item => item.Name == FineGrainedPermissions.Integrations.ManageContracts)
            ?? await context.Permissions.FindAsync(ManageContractsPermissionId);

        if (permission is null)
        {
            permission = new Permission { Id = ManageContractsPermissionId };
            context.Permissions.Add(permission);
        }

        permission.Name = FineGrainedPermissions.Integrations.ManageContracts;
        permission.Description = "Administrar contratos de integración";

        await context.SaveChangesAsync();
    }
}
