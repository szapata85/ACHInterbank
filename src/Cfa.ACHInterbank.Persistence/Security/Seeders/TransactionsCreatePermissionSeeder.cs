using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Persists the already-defined fine-grained Transactions.Create permission so navigation
/// can reference the same creation capability accepted by P0.TransactionsCreate.
/// This seeder does not grant the permission to any role or user.
/// </summary>
[Scoped]
public sealed class TransactionsCreatePermissionSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid PermissionId = Guid.Parse("5c31592a-98cf-4ea2-a5b4-47e9c0803f42");

    public int Order => 0;

    public async Task SeedAsync()
    {
        var permission = await context.Permissions
            .FirstOrDefaultAsync(item => item.Name == FineGrainedPermissions.Transactions.Create)
            ?? await context.Permissions.FindAsync(PermissionId);

        if (permission is null)
        {
            permission = new Permission { Id = PermissionId };
            context.Permissions.Add(permission);
        }

        permission.Name = FineGrainedPermissions.Transactions.Create;
        permission.Description = "Crear transacciones ACH";

        await context.SaveChangesAsync();
    }
}
