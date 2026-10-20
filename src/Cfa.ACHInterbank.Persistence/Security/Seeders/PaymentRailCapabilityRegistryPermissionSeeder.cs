using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Persists the already-defined read capability for the payment-rail capability registry.
/// NAV3.4I deliberately creates no role or direct user grants; CanManageAch and CanReadAch
/// remain temporary policy/menu alternatives until a later explicit grant migration.
/// </summary>
[Scoped]
public sealed class PaymentRailCapabilityRegistryPermissionSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid PermissionId = Guid.Parse("9c178737-a650-4437-a68b-9b3f21555e02");

    public int Order => 0;

    public async Task SeedAsync()
    {
        var permission = await context.Permissions
            .FirstOrDefaultAsync(item => item.Name == FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)
            ?? await context.Permissions.FindAsync(PermissionId);

        if (permission is null)
        {
            permission = new Permission { Id = PermissionId };
            context.Permissions.Add(permission);
        }

        permission.Name = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry;
        permission.Description = "Consultar capacidades de la plataforma";

        await context.SaveChangesAsync();
    }
}
