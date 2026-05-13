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
        await EnsureClearingHousesAsync(context);
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
        var clearingHouse = await EnsureClearingHousesAsync(context);

        context.AchReturnCodes.Add(new AchReturnCode
        {
            ClearingHouseId = clearingHouse.Cenit.Id,
            FlowType = AchReturnFlowType.Any,
            EffectiveFrom = DateTime.UtcNow.Date.AddDays(-30),
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


    private static async Task<(ClearingHouse Cenit, ClearingHouse AchColombia)> EnsureClearingHousesAsync(AchDbContext context)
    {
        var cenitExisting = await context.ClearingHouses.FirstOrDefaultAsync(x => x.Code == "CENIT");
        var achExisting = await context.ClearingHouses.FirstOrDefaultAsync(x => x.Code == "ACH");
        if (cenitExisting is not null && achExisting is not null) return (cenitExisting, achExisting);

        var configCenit = new ClearingHouseConfig { ClearingHouseId = 5001, HolidayStrategy = "Colombian" };
        var configAch = new ClearingHouseConfig { ClearingHouseId = 5002, HolidayStrategy = "Colombian" };
        context.ClearingHouseConfigs.AddRange(configCenit, configAch);
        await context.SaveChangesAsync();

        var cenit = cenitExisting ?? new ClearingHouse { Name = "CENIT", Code = "CENIT", OriginCode = "000101006", ClearingHouseId = configCenit.Id };
        var ach = achExisting ?? new ClearingHouse { Name = "ACH Colombia", Code = "ACH", OriginCode = "000101007", ClearingHouseId = configAch.Id };
        if (cenitExisting is null) context.ClearingHouses.Add(cenit);
        if (achExisting is null) context.ClearingHouses.Add(ach);
        await context.SaveChangesAsync();
        return (cenit, ach);
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
}
