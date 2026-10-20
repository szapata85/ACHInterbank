using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class NavigationMenuSeedTests
{
    [Fact]
    public async Task MainMenuSeed_ShouldNotExposeLegacyClearingHouseRulesRoute()
    {
        const int LegacyClearingHouseTransactionRulesId = 32;
        await using var context = CreateContext();

        Assert.False(await context.MenuItems
            .AsNoTracking()
            .AnyAsync(x => x.Route == "/transactions/clearing-house-rules"));
        Assert.False(await context.MenuItemRoles
            .AsNoTracking()
            .AnyAsync(x => x.MenuItemId == LegacyClearingHouseTransactionRulesId));
        Assert.False(await context.MenuItemPermissions
            .AsNoTracking()
            .AnyAsync(x => x.MenuItemId == LegacyClearingHouseTransactionRulesId));
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
