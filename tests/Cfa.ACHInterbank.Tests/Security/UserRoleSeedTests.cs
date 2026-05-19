using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests.Security;

public class UserRoleSeedTests
{
    [Fact]
    public void AdminSeed_DebeTenerRolesAdminYAChOperator()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase($"user-role-seed-{Guid.NewGuid()}")
            .Options;

        using var context = new AchDbContext(options);

        context.Database.EnsureCreated();

        var adminRoleIds = context.UserRoles
            .Where(userRole => userRole.UserId == UserConfiguration.AdminUserId)
            .Select(userRole => userRole.RoleId)
            .ToList();

        Assert.Contains(RoleConfiguration.AdminRoleId, adminRoleIds);
        Assert.Contains(RoleConfiguration.OperatorRoleId, adminRoleIds);
    }
}
