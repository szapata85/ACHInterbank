using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Materializes the pre-existing mapping capabilities used by NAV3.4P. No role or user
/// grant is created; CanManageAch remains the compatibility fallback in the P1 policies.
/// </summary>
[Scoped]
public sealed class IntegrationMappingPermissionSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid ManageMappingsPermissionId = Guid.Parse("a78e21ac-846c-470d-a9a5-5d4bd019bf08");
    public static readonly Guid ValidatePermissionId = Guid.Parse("67d58589-d51e-44b2-8177-2650d93bc46d");
    public static readonly Guid PublishPermissionId = Guid.Parse("911b72f4-ef59-48e4-86de-7d559dbafb1d");

    public int Order => 0;

    public async Task SeedAsync()
    {
        await UpsertAsync(ManageMappingsPermissionId, FineGrainedPermissions.Integrations.ManageMappings, "Administrar mapeos de integración");
        await UpsertAsync(ValidatePermissionId, FineGrainedPermissions.Integrations.Validate, "Validar mapeos de integración");
        await UpsertAsync(PublishPermissionId, FineGrainedPermissions.Integrations.Publish, "Publicar mapeos de integración");
        await context.SaveChangesAsync();
    }

    private async Task UpsertAsync(Guid id, string name, string description)
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
