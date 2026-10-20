using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Persists the already-defined Config.Read and Config.Manage capabilities used by the P1
/// configuration policies. NAV3.4M does not grant either permission to any role or user.
/// </summary>
[Scoped]
public sealed class ConfigurationPermissionSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid ReadPermissionId = Guid.Parse("15d0e235-eb36-4f7d-a922-d935dc1af7e2");
    public static readonly Guid ManagePermissionId = Guid.Parse("6e9d334b-d77a-4576-814f-8e1f987a421f");

    public int Order => 0;

    public async Task SeedAsync()
    {
        await UpsertAsync(
            ReadPermissionId,
            FineGrainedPermissions.Config.Read,
            "Consultar configuración de la plataforma");
        await UpsertAsync(
            ManagePermissionId,
            FineGrainedPermissions.Config.Manage,
            "Administrar configuración de la plataforma");
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
