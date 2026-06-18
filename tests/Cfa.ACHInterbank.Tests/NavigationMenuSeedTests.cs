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

    [Fact]
    public async Task MainMenuSeed_ShouldExposeNachaInboundSimulatorUnderUat()
    {
        await using var context = CreateContext();

        var menuItem = await context.MenuItems
            .AsNoTracking()
            .SingleAsync(x => x.Route == "/uat/nacha-inbound-simulator");

        Assert.Equal(MenuItemConfiguration.NachaInboundSimulatorId, menuItem.Id);
        Assert.Equal(MenuItemConfiguration.UatSimulatorsId, menuItem.ParentId);
        Assert.True(menuItem.IsActive);

        Assert.True(await context.MenuItemRoles.AnyAsync(x =>
            x.MenuItemId == MenuItemConfiguration.NachaInboundSimulatorId
            && x.RoleId == RoleConfiguration.AdminRoleId));
        Assert.True(await context.MenuItemRoles.AnyAsync(x =>
            x.MenuItemId == MenuItemConfiguration.NachaInboundSimulatorId
            && x.RoleId == RoleConfiguration.OperatorRoleId));
    }

    [Fact]
    public async Task MainMenuSeed_ShouldNotDuplicateNachaConfigProfileRoute()
    {
        await using var context = CreateContext();

        var nachaConfigGroup = await context.MenuItems
            .AsNoTracking()
            .SingleAsync(x => x.Id == MenuItemConfiguration.NachaLayoutsId);
        var profilesItem = await context.MenuItems
            .AsNoTracking()
            .SingleAsync(x => x.Id == MenuItemConfiguration.NachaDefinitionsId);
        var profileRouteItems = await context.MenuItems
            .AsNoTracking()
            .Where(x => x.Route == "/nacha-config-admin/perfiles")
            .ToListAsync();

        Assert.Equal("/nacha-config-admin", nachaConfigGroup.Route);
        Assert.Equal("/nacha-config-admin/perfiles", profilesItem.Route);
        Assert.Equal(MenuItemConfiguration.NachaLayoutsId, profilesItem.ParentId);
        Assert.Single(profileRouteItems);
        Assert.Equal(MenuItemConfiguration.NachaDefinitionsId, profileRouteItems[0].Id);
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
