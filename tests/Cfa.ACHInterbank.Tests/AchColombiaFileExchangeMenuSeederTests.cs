using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaFileExchangeMenuSeederTests
{
    [Fact]
    public async Task Seeder_ShouldCreateSpanishMenuEntryWithRolesAndPermission()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase($"menu-{Guid.NewGuid():N}").Options;
        await using var context = new AchDbContext(options);
        context.MenuItems.Add(new MenuItem { Id = 1, MenuId = 1, Label = "Transacciones", Route = "/transactions", Icon = "swap_horiz", Order = 1, IsActive = true });
        await context.SaveChangesAsync();
        await new AchColombiaFileExchangeMenuSeeder(context).SeedAsync();
        var item = await context.MenuItems.SingleAsync(x => x.Route == AchColombiaFileExchangeMenuSeeder.Route);
        Assert.Equal("Intercambio de archivos ACH Colombia", item.Label);
        Assert.Equal(2, await context.MenuItemRoles.CountAsync(x => x.MenuItemId == item.Id));
        Assert.Single(await context.MenuItemPermissions.Where(x => x.MenuItemId == item.Id).ToListAsync());
    }
}
