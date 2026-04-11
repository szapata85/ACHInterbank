using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class ClearingHouseCycleConfigSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesUsefulScenariosForAchAndCenit()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AchDbContext(options);
        context.Database.EnsureCreated();

        context.ClearingHouses.AddRange(
            new ClearingHouse { Id = 1, Name = "ACH Colombia", Code = "ACHCOL", OriginCode = "12345678", ClearingHouseId = 1 },
            new ClearingHouse { Id = 2, Name = "CENIT", Code = "CENIT", OriginCode = "87654321", ClearingHouseId = 1 });

        await context.SaveChangesAsync();

        var seeder = new ClearingHouseCycleConfigSeeder(context);
        await seeder.SeedAsync();

        var all = await context.ClearingHouseCycleConfigs.AsNoTracking().ToListAsync();

        Assert.NotEmpty(all);
        Assert.Contains(all, c => c.ClearingHouseId == 1);
        Assert.Contains(all, c => c.ClearingHouseId == 2);
        Assert.Contains(all, c => !c.IsActive);
        Assert.Contains(all, c => c.EffectiveFrom.Year >= 2026);
        Assert.True(all.Where(c => c.ClearingHouseId == 1).Select(c => c.CycleName).Distinct().Count() >= 5);
        Assert.True(all.Where(c => c.ClearingHouseId == 2).Select(c => c.CycleName).Distinct().Count() >= 3);

        var referenceDate = new DateTime(2026, 8, 1);
        var cenitCycle2Current = all.Where(c => c.ClearingHouseId == 2 &&
                                                c.CycleName == "Ciclo 2" &&
                                                c.EffectiveFrom.Date <= referenceDate.Date &&
                                                (!c.EffectiveTo.HasValue || c.EffectiveTo.Value.Date >= referenceDate.Date));

        Assert.Single(cenitCycle2Current);
    }
}
