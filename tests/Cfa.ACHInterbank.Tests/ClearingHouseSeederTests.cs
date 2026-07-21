using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class ClearingHouseSeederTests
{
    [Fact]
    public async Task SeedAsync_CompletesPartialSeedUsingTheExistingConfiguration()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 9, HolidayStrategy = "Colombian" });
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 42,
            Name = "ACH Colombia",
            Code = "ACHCOL",
            OriginCode = "000101006",
            ClearingHouseId = 9
        });
        await context.SaveChangesAsync();

        var seeder = new ClearingHouseSeeder(context);
        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var clearingHouses = await context.ClearingHouses.OrderBy(house => house.Code).ToListAsync();
        Assert.Collection(clearingHouses,
            house => Assert.Equal("ACHCOL", house.Code),
            house =>
            {
                Assert.Equal("CENIT", house.Code);
                Assert.NotEqual(9, house.ClearingHouseId);
            });

        Assert.Equal(3, await context.ClearingHouseConfigs.CountAsync());
        Assert.All(clearingHouses, house =>
            Assert.True(context.ClearingHouseConfigs.Any(config => config.Id == house.ClearingHouseId && config.ClearingHouseId == house.Id)));
    }
}
