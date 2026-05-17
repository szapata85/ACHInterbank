using Cfa.ACHInterbank.Domain.Models.Configurations;
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
        await EnsureClearingHouseAsync(context, "CENIT", "CENIT");
        await EnsureClearingHouseAsync(context, "ACH", "ACH Colombia");

        var sut = new RegulatoryCatalogSeeder(context);
        await sut.SeedAsync();
    }

    [Fact]
    public async Task Seeder_ShouldFail_WhenCenitClearingHouseIsMissing()
    {
        await using var context = await CreateContextAsync();
        await EnsureClearingHouseAsync(context, "ACH", "ACH Colombia");

        var sut = new RegulatoryCatalogSeeder(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SeedAsync());

        Assert.Contains("CENIT", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Seeder_ShouldFail_WhenAchColombiaClearingHouseIsMissing()
    {
        await using var context = await CreateContextAsync();
        await EnsureClearingHouseAsync(context, "CENIT", "CENIT");

        var sut = new RegulatoryCatalogSeeder(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SeedAsync());

        Assert.Contains("ACH Colombia", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<AchDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task EnsureClearingHouseAsync(AchDbContext context, string code, string name)
    {
        var existing = await context.ClearingHouses.FirstOrDefaultAsync(x => x.Code == code);
        if (existing is not null) return;

        var config = new ClearingHouseConfig
        {
            ClearingHouseId = 9000 + Math.Abs(code.GetHashCode() % 1000),
            HolidayStrategy = "Colombian"
        };

        context.ClearingHouseConfigs.Add(config);
        await context.SaveChangesAsync();

        context.ClearingHouses.Add(new ClearingHouse
        {
            Name = name,
            Code = code,
            OriginCode = "000101006",
            ClearingHouseId = config.Id
        });

        await context.SaveChangesAsync();
    }
}
