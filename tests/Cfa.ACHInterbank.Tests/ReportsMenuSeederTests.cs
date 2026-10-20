using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class ReportsMenuSeederTests
{
    [Fact]
    public async Task SeedAsync_ShouldCreateAuthorizedReportsEntryIdempotently()
    {
        await using var context = CreateContext();
        var seeder = new ReportsMenuSeeder(context);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var reports = await context.MenuItems
            .SingleAsync(item => item.Route == ReportsMenuSeeder.Route);

        Assert.Equal("Reportes", reports.Label);
        Assert.Equal("analytics", reports.Icon);
        Assert.True(reports.IsActive);
        Assert.Null(reports.ParentId);
        Assert.Equal(7, reports.Order);
        Assert.True(await context.MenuItemPermissions.AnyAsync(link =>
            link.MenuItemId == reports.Id
            && link.PermissionId == PermissionConfiguration.ReadAchPermissionId));
        Assert.Equal(2, await context.MenuItemRoles.CountAsync(link =>
            link.MenuItemId == reports.Id
            && (link.RoleId == RoleConfiguration.AdminRoleId || link.RoleId == RoleConfiguration.OperatorRoleId)));
    }

    private static AchDbContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options);
        context.Database.EnsureCreated();
        return context;
    }
}
