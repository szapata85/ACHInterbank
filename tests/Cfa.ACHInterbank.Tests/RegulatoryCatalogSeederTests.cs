using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
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
        Assert.True(await context.ClearingHouseTransactionRules.AnyAsync(x =>
            x.TransactionNature == TransactionNature.Debit
            && x.RequiresPrenotification
            && x.PrenotificationMode == PrenotificationRequirementMode.Mandatory
            && x.PrenotificationLeadBusinessDays == 3
            && x.NormativeSource.Contains("MAN-004")));
        Assert.True(await context.ClearingHouseTransactionRules.AnyAsync(x =>
            x.TransactionNature == TransactionNature.Credit
            && !x.RequiresPrenotification
            && x.PrenotificationMode == PrenotificationRequirementMode.Optional
            && x.NormativeSource.Contains("CENIT")));
        Assert.True(await context.AchFileRejectionCodes.AnyAsync(x => x.Code == "ITIMEOUT" && x.IsRetryable));
        Assert.True(await context.AchFileRejectionCodes.AnyAsync(x => x.Code == "IFUNC" && !x.IsRetryable));
    }

    [Fact]
    public async Task SeedAsync_IsInsertOnlyAndPreservesAdministrativeChanges()
    {
        await using var context = await CreateContextAsync();
        var sut = new RegulatoryCatalogSeeder(context);
        await sut.SeedAsync();

        Assert.Equal(4, await context.ClearingHouseTransactionRules.CountAsync());
        var policy = await context.ClearingHouseTransactionRules
            .Include(x => x.ClearingHouse)
            .SingleAsync(x => x.ClearingHouse.Code == "ACHCOL" && x.TransactionNature == TransactionNature.Debit);

        policy.IsActive = false;
        policy.PrenotificationMode = PrenotificationRequirementMode.Optional;
        policy.RequiresPrenotification = false;
        policy.PrenotificationLeadBusinessDays = null;
        policy.EffectiveTo = new DateTime(2028, 12, 31);
        policy.NormativeReference = "CAMBIO-ADMINISTRATIVO";
        policy.Notes = "No debe ser sobrescrito por bootstrap.";
        await context.SaveChangesAsync();

        await sut.SeedAsync();
        context.ChangeTracker.Clear();

        var persisted = await context.ClearingHouseTransactionRules
            .Include(x => x.ClearingHouse)
            .SingleAsync(x => x.ClearingHouse.Code == "ACHCOL" && x.TransactionNature == TransactionNature.Debit);
        Assert.Equal(4, await context.ClearingHouseTransactionRules.CountAsync());
        Assert.False(persisted.IsActive);
        Assert.Equal(PrenotificationRequirementMode.Optional, persisted.PrenotificationMode);
        Assert.False(persisted.RequiresPrenotification);
        Assert.Null(persisted.PrenotificationLeadBusinessDays);
        Assert.Equal(new DateTime(2028, 12, 31), persisted.EffectiveTo);
        Assert.Equal("CAMBIO-ADMINISTRATIVO", persisted.NormativeReference);
        Assert.Equal("No debe ser sobrescrito por bootstrap.", persisted.Notes);
    }

    [Fact]
    public async Task Seeder_ShouldFailClearly_WhenNoClearingHouseExists()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        await using var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        context.ClearingHouses.RemoveRange(context.ClearingHouses);
        await context.SaveChangesAsync();

        var sut = new RegulatoryCatalogSeeder(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SeedAsync());
        Assert.Contains("CENIT", ex.Message, StringComparison.OrdinalIgnoreCase);
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
        await EnsureClearingHouseAsync(context, "CENIT", "CENIT");
        await EnsureClearingHouseAsync(context, "ACHCOL", "ACH Colombia");
        return context;
    }

    private static async Task<ClearingHouse> EnsureClearingHouseAsync(AchDbContext context, string code = "CENIT", string name = "CENIT")
    {
        var existing = await context.ClearingHouses.FirstOrDefaultAsync(x => x.Code == code);
        if (existing is not null) return existing;

        var config = new ClearingHouseConfig { ClearingHouseId = 9000 + Math.Abs(code.GetHashCode() % 1000), HolidayStrategy = "Colombian" };
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
