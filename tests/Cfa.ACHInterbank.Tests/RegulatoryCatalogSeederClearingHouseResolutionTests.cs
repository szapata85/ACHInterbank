using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class RegulatoryCatalogSeederClearingHouseResolutionTests
{
    [Fact]
    public async Task Seeder_ShouldResolveCenitAndAchColombiaClearingHouses()
    {
        await using var context = await CreateContextAsync();
        await CreateCenitAsync(context);
        await CreateAchAsync(context);

        var sut = new RegulatoryCatalogSeeder(context);
        await sut.SeedAsync();
    }

    [Fact]
    public async Task Seeder_ShouldFail_WhenCenitClearingHouseIsMissing()
    {
        await using var context = await CreateContextAsync();
        await CreateAchAsync(context);

        var sut = new RegulatoryCatalogSeeder(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SeedAsync());
        Assert.Contains("CENIT", ex.Message);
    }

    [Fact]
    public async Task Seeder_ShouldFail_WhenAchColombiaClearingHouseIsMissing()
    {
        await using var context = await CreateContextAsync();
        await CreateCenitAsync(context);

        var sut = new RegulatoryCatalogSeeder(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SeedAsync());
        Assert.Contains("ACH Colombia", ex.Message);
    }

    private static async Task CreateCenitAsync(AchDbContext context)
    {
        var cfg = new ClearingHouseConfig { ClearingHouseId = 6001, HolidayStrategy = "Col" };
        context.ClearingHouseConfigs.Add(cfg);
        await context.SaveChangesAsync();
        context.ClearingHouses.Add(new ClearingHouse { Name = "CENIT", Code = "CENIT", OriginCode = "000101006", ClearingHouseId = cfg.Id });
        await context.SaveChangesAsync();
    }

    private static async Task CreateAchAsync(AchDbContext context)
    {
        var cfg = new ClearingHouseConfig { ClearingHouseId = 6002, HolidayStrategy = "Col" };
        context.ClearingHouseConfigs.Add(cfg);
        await context.SaveChangesAsync();
        context.ClearingHouses.Add(new ClearingHouse { Name = "ACH Colombia", Code = "ACH", OriginCode = "000101007", ClearingHouseId = cfg.Id });
        await context.SaveChangesAsync();
    }

    private static async Task<AchDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
