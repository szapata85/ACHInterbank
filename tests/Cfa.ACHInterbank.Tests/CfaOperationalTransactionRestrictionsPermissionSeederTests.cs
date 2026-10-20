using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class CfaOperationalTransactionRestrictionsPermissionSeederTests
{
    [Fact]
    public async Task SeedAsync_IsIdempotent_AndAssignsReadManageWithLeastPrivilegeDefaults()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        var sut = new CfaOperationalTransactionRestrictionsPermissionSeeder(context);

        await sut.SeedAsync();
        await sut.SeedAsync();

        var permissions = await context.Permissions
            .Where(x => x.Name == FineGrainedPermissions.OperationalRestrictions.Read
                || x.Name == FineGrainedPermissions.OperationalRestrictions.Manage)
            .ToListAsync();
        Assert.Equal(2, permissions.Count);

        var readId = permissions.Single(x => x.Name == FineGrainedPermissions.OperationalRestrictions.Read).Id;
        var manageId = permissions.Single(x => x.Name == FineGrainedPermissions.OperationalRestrictions.Manage).Id;
        Assert.Single(await context.RolePermissions
            .Where(x => x.RoleId == RoleConfiguration.AdminRoleId && x.PermissionId == readId)
            .ToListAsync());
        Assert.Single(await context.RolePermissions
            .Where(x => x.RoleId == RoleConfiguration.OperatorRoleId && x.PermissionId == readId)
            .ToListAsync());
        Assert.Single(await context.RolePermissions
            .Where(x => x.RoleId == RoleConfiguration.AdminRoleId && x.PermissionId == manageId)
            .ToListAsync());
        Assert.Empty(await context.RolePermissions
            .Where(x => x.RoleId == RoleConfiguration.OperatorRoleId && x.PermissionId == manageId)
            .ToListAsync());
    }
}
