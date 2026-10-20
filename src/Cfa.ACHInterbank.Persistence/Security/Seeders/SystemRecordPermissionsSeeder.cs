using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Persists the dedicated read capabilities for administrative system records.
/// NAV3.4G2 deliberately creates no role or direct user grants; legacy permissions
/// remain policy/menu alternatives until a later explicit grant migration.
/// </summary>
[Scoped]
public sealed class SystemRecordPermissionsSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid AuditReadPermissionId = Guid.Parse("bafe62e5-5984-4138-9c02-4621443b5286");
    public static readonly Guid AuthReadPermissionId = Guid.Parse("5e97df51-c57d-4855-b670-757fa4b5b6f6");
    public static readonly Guid NavigationReadPermissionId = Guid.Parse("5de95eb2-18d0-4d9a-86c5-265f0a717bcb");

    public int Order => 0;

    public async Task SeedAsync()
    {
        await EnsurePermissionAsync(AuditReadPermissionId, FineGrainedPermissions.AuditLogs.Read, "Consultar registro de auditoría");
        await EnsurePermissionAsync(AuthReadPermissionId, FineGrainedPermissions.AuthLogs.Read, "Consultar registro de accesos");
        await EnsurePermissionAsync(NavigationReadPermissionId, FineGrainedPermissions.NavigationLogs.Read, "Consultar registro de navegación");
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
