using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class RegulatoryCatalogSeederTests
{
    [Fact]
    public async Task SeedAsync_ShouldPopulateBaselineInboundCatalogs_WhenDatabaseIsEmpty()
    {
        await using var context = await CreateContextAsync();
        var sut = new RegulatoryCatalogSeeder(context);

        await sut.SeedAsync();

        Assert.True(await context.AchReturnCodes.AnyAsync(x => x.Code == "DEV14" && x.IsActive));
        Assert.True(await context.AchReturnPolicies.AnyAsync(x => x.TransactionType == "Debit" && x.IsActive));
        Assert.True(await context.AchPrenotificationPolicies.AnyAsync(x => x.TransactionType == "Debit" && x.IsRequired));
        Assert.True(await context.AchFileRejectionCodes.AnyAsync(x => x.Code == "ITIMEOUT" && x.IsRetryable));
        Assert.True(await context.AchFileRejectionCodes.AnyAsync(x => x.Code == "IFUNC" && !x.IsRetryable));
    }

    [Fact]
    public async Task SeedAsync_ShouldRepairDriftAndInsertMissingRows_WhenCatalogsArePartial()
    {
        await using var context = await CreateContextAsync();
        var clearingHouse = await EnsureClearingHouseAsync(context);

        context.AchReturnCodes.Add(new AchReturnCode
        {
            ClearingHouseId = clearingHouse.Id,
            Code = "R01",
            Description = "legacy",
            AppliesToDebit = false,
            AppliesToCredit = false,
            AppliesToPrenotification = false,
            AppliesToReturn = false,
            RequiresAddenda = false,
            MaxDaysAllowed = 99,
            RegulatorySource = "LEGACY",
            IsActive = false
        });

        context.AchFileRejectionCodes.Add(new AchFileRejectionCode
        {
            Code = "D01",
            Description = "legacy",
            Severity = "Info",
            AppliesToStage = "Legacy",
            IsRetryable = true,
            IsActive = false
        });

        await context.SaveChangesAsync();

        var sut = new RegulatoryCatalogSeeder(context);
        await sut.SeedAsync();

        var repairedR01 = await context.AchReturnCodes.AsNoTracking().SingleAsync(x => x.Code == "R01");
        Assert.True(repairedR01.IsActive);
        Assert.True(repairedR01.AppliesToDebit);
        Assert.Equal("CENIT", repairedR01.RegulatorySource);
        Assert.NotEqual("legacy", repairedR01.Description);

        var repairedD01 = await context.AchFileRejectionCodes.AsNoTracking().SingleAsync(x => x.Code == "D01");
        Assert.Equal("Validation", repairedD01.AppliesToStage);
        Assert.Equal("Fatal", repairedD01.Severity);
        Assert.False(repairedD01.IsRetryable);
        Assert.True(repairedD01.IsActive);

        Assert.True(await context.AchReturnCodes.AnyAsync(x => x.Code == "DEV14"));
        Assert.True(await context.AchFileRejectionCodes.AnyAsync(x => x.Code == "I503" && x.IsRetryable));
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
        await EnsureClearingHouseAsync(context);
        return context;
    }

    private static async Task<ClearingHouse> EnsureClearingHouseAsync(AchDbContext context, string code = "CENIT", string name = "CENIT")
    {
        var existing = await context.ClearingHouses.FirstOrDefaultAsync(x => x.Code == code);
        if (existing is not null) return existing;

        var config = new ClearingHouseConfig { ClearingHouseId = 9001, HolidayStrategy = "Colombian" };
        context.ClearingHouseConfigs.Add(config);
        await context.SaveChangesAsync();

        var clearingHouse = new ClearingHouse
        {
            Name = name,
            Code = code,
            OriginCode = "000101006",
            ClearingHouseId = config.Id
        };

        context.ClearingHouses.Add(clearingHouse);
        await context.SaveChangesAsync();
        return clearingHouse;
    }
}
