using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class NavigationMenuSeedTests
{
    [Fact]
    public async Task MainMenuSeed_ShouldExposeClearingHouseRulesUnderTransactions()
    {
        await using var context = CreateContext();

        var menuItem = await context.MenuItems
            .AsNoTracking()
            .SingleAsync(x => x.Route == "/transactions/clearing-house-rules");

        Assert.Equal(MenuItemConfiguration.ClearingHouseTransactionRulesId, menuItem.Id);
        Assert.Equal(MenuItemConfiguration.TransactionsId, menuItem.ParentId);
        Assert.True(menuItem.IsActive);

        Assert.True(await context.MenuItemRoles.AnyAsync(x =>
            x.MenuItemId == MenuItemConfiguration.ClearingHouseTransactionRulesId
            && x.RoleId == RoleConfiguration.AdminRoleId));
        Assert.True(await context.MenuItemRoles.AnyAsync(x =>
            x.MenuItemId == MenuItemConfiguration.ClearingHouseTransactionRulesId
            && x.RoleId == RoleConfiguration.OperatorRoleId));
        Assert.True(await context.MenuItemPermissions.AnyAsync(x =>
            x.MenuItemId == MenuItemConfiguration.ClearingHouseTransactionRulesId
            && x.PermissionId == PermissionConfiguration.ManageAchPermissionId));
        Assert.True(await context.MenuItemPermissions.AnyAsync(x =>
            x.MenuItemId == MenuItemConfiguration.ClearingHouseTransactionRulesId
            && x.PermissionId == PermissionConfiguration.ReadAchPermissionId));
    }

    private static AchDbContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
