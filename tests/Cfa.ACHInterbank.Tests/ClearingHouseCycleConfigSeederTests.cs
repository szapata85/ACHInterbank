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

        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 10, HolidayStrategy = "Colombian" });

        context.ClearingHouses.AddRange(
            new ClearingHouse { Id = 31, Name = "ACH Colombia", Code = "ACHCOL", OriginCode = "12345678", ClearingHouseId = 10 },
            new ClearingHouse { Id = 47, Name = "CENIT", Code = "CENIT", OriginCode = "87654321", ClearingHouseId = 10 });

        await context.SaveChangesAsync();

        var seeder = new ClearingHouseCycleConfigSeeder(context);
        await seeder.SeedAsync();

        var all = await context.ClearingHouseCycleConfigs.AsNoTracking().ToListAsync();

        Assert.NotEmpty(all);
        Assert.Contains(all, c => c.ClearingHouseId == 31);
        Assert.Contains(all, c => c.ClearingHouseId == 47);
        Assert.Contains(all, c => !c.IsActive);
        Assert.Contains(all, c => c.EffectiveFrom.Year >= 2026);
        Assert.True(all.Where(c => c.ClearingHouseId == 31).Select(c => c.CycleName).Distinct().Count() >= 5);
        Assert.True(all.Where(c => c.ClearingHouseId == 47).Select(c => c.CycleName).Distinct().Count() >= 3);

        var referenceDate = new DateTime(2026, 8, 1);
        var cenitCycle2Current = all.Where(c => c.ClearingHouseId == 47 &&
                                                c.CycleName == "Ciclo 2" &&
                                                c.EffectiveFrom.Date <= referenceDate.Date &&
                                                (!c.EffectiveTo.HasValue || c.EffectiveTo.Value.Date >= referenceDate.Date));

        Assert.Single(cenitCycle2Current);

        await seeder.SeedAsync();
        Assert.Equal(all.Count, await context.ClearingHouseCycleConfigs.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_CompletesPartialCyclesWithoutDuplicatingExistingRecords()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        using var context = new AchDbContext(options);
        context.Database.EnsureCreated();

        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 7, HolidayStrategy = "Colombian" });
        context.ClearingHouses.AddRange(
            new ClearingHouse { Id = 12, Name = "ACH Colombia", Code = "ACHCOL", OriginCode = "12345678", ClearingHouseId = 7 },
            new ClearingHouse { Id = 24, Name = "CENIT", Code = "CENIT", OriginCode = "87654321", ClearingHouseId = 7 });
        context.ClearingHouseCycleConfigs.Add(new ClearingHouseCycleConfig
        {
            ClearingHouseId = 12,
            CycleName = "Ciclo 1",
            StartTime = new TimeSpan(19, 1, 0),
            EndTime = new TimeSpan(8, 30, 0),
            CutoffTime = new TimeSpan(8, 30, 0),
            IsActive = false,
            EffectiveFrom = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc),
            EffectiveTo = DateTime.SpecifyKind(new DateTime(2024, 12, 31), DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        await new ClearingHouseCycleConfigSeeder(context).SeedAsync();

        Assert.Equal(14, await context.ClearingHouseCycleConfigs.CountAsync());
        Assert.Equal(1, await context.ClearingHouseCycleConfigs.CountAsync(c =>
            c.ClearingHouseId == 12 && c.CycleName == "Ciclo 1" && c.EffectiveFrom.Year == 2024));
    }
}
