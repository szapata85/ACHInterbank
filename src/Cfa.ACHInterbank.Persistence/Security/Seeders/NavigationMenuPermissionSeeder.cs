using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Persists the dedicated capability for administration of the navigation menu.
/// NAV3.4H intentionally creates no role or direct user grant; CanManageUsers
/// remains a temporary policy/menu fallback until a later explicit grant migration.
/// </summary>
[Scoped]
public sealed class NavigationMenuPermissionSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid PermissionId = Guid.Parse("5f847759-419c-4cf5-b2bb-2a849b8fa33c");

    public int Order => 0;

    public async Task SeedAsync()
    {
        var permission = await context.Permissions
            .FirstOrDefaultAsync(item => item.Name == FineGrainedPermissions.NavigationMenu.Manage)
            ?? await context.Permissions.FindAsync(PermissionId);

        if (permission is null)
        {
            permission = new Permission { Id = PermissionId };
            context.Permissions.Add(permission);
        }

        permission.Name = FineGrainedPermissions.NavigationMenu.Manage;
        permission.Description = "Administrar organización del menú";

        await context.SaveChangesAsync();
    }
}
