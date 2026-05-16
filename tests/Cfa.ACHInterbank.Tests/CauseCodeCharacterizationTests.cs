using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class CauseCodeCharacterizationTests
{
    [Fact]
    public async Task RegulatoryCatalogSeeder_ShouldSeedAchColombiaReturnCodes_CurrentCatalog()
    {
        await using var context = await CreateContextAsync();
        var (_, ach) = await SeedClearingHousesAsync(context);
        await new RegulatoryCatalogSeeder(context).SeedAsync();

        var achCodes = await context.AchReturnCodes.Where(x => x.ClearingHouseId == ach.Id).Select(x => x.Code).ToListAsync();
        Assert.Contains("R07", achCodes);
        Assert.Contains("R10", achCodes);
        Assert.Contains("R29", achCodes);
        Assert.Contains("R31", achCodes);
        Assert.Contains("DEV14", achCodes);
    }

    [Fact]
    public async Task RegulatoryCatalogSeeder_ShouldSeedCenitReturnCodes_CurrentCatalog()
    {
        await using var context = await CreateContextAsync();
        var (cenit, _) = await SeedClearingHousesAsync(context);
        await new RegulatoryCatalogSeeder(context).SeedAsync();

        var cenitCodes = await context.AchReturnCodes.Where(x => x.ClearingHouseId == cenit.Id).Select(x => x.Code).ToListAsync();
        foreach (var code in new[] { "R01", "R02", "R03", "R04", "R06", "R08", "R09", "R12", "R13", "R14", "R15", "R16", "R17", "R20", "R23" })
            Assert.Contains(code, cenitCodes);
    }

    [Fact]
    public async Task RegulatoryCatalogSeeder_ShouldSeedFileRejectionAndTechnicalCodes_CurrentCatalog()
    {
        await using var context = await CreateContextAsync();
        await SeedClearingHousesAsync(context);
        await new RegulatoryCatalogSeeder(context).SeedAsync();

        var codes = await context.AchFileRejectionCodes.Select(x => x.Code).ToListAsync();
        foreach (var code in new[] { "D01", "D02", "D03", "D04", "D05", "D06", "I500", "I503", "ITIMEOUT", "ISOAP", "IFUNC" })
            Assert.Contains(code, codes);
    }

    [Fact]
    public async Task CauseCodeCharacterization_ShouldTreatDxxIxxxAsNonReturnReasons()
    {
        await using var context = await CreateContextAsync();
        await SeedClearingHousesAsync(context);
        await new RegulatoryCatalogSeeder(context).SeedAsync();

        var returnCodes = await context.AchReturnCodes.Select(x => x.Code).ToListAsync();
        Assert.DoesNotContain("D01", returnCodes);
        Assert.DoesNotContain("I500", returnCodes);
    }

    [Fact]
    public async Task AchRegulatoryCatalogService_ShouldAcceptAchReturnCodes_ForAchRail_CurrentCatalog()
    {
        await using var context = await CreateContextAsync();
        var (_, ach) = await SeedClearingHousesAsync(context);
        await new RegulatoryCatalogSeeder(context).SeedAsync();
        var sut = new AchRegulatoryCatalogService(context);

        foreach (var code in new[] { "R07", "R10", "R29", "R31", "DEV14" })
        {
            var result = await sut.ValidateReturnCodeAsync(ach.Id, code, TransactionTypeEnum.Debit, DateTime.UtcNow.Date, DateTime.UtcNow.Date, CancellationToken.None);
            Assert.True(result.IsAllowed, $"ACH code should be allowed in current catalog: {code}");
        }
    }

    [Fact]
    public async Task AchRegulatoryCatalogService_ShouldAcceptCenitReturnCodes_ForCenitRail_CurrentCatalog()
    {
        await using var context = await CreateContextAsync();
        var (cenit, _) = await SeedClearingHousesAsync(context);
        await new RegulatoryCatalogSeeder(context).SeedAsync();
        var sut = new AchRegulatoryCatalogService(context);

        foreach (var code in new[] { "R01", "R02", "R03", "R04", "R06", "R08", "R09", "R12", "R13", "R14", "R15", "R16", "R17", "R20", "R23" })
        {
            var result = await sut.ValidateReturnCodeAsync(cenit.Id, code, TransactionTypeEnum.Debit, DateTime.UtcNow.Date, DateTime.UtcNow.Date, CancellationToken.None);
            Assert.True(result.IsAllowed, $"CENIT code should be allowed in current catalog: {code}");
        }
    }

    [Fact]
    public async Task AchRegulatoryCatalogService_ShouldRejectCenitCode_ForAchRail_WhenNotConfigured()
    {
        await using var context = await CreateContextAsync();
        var (_, ach) = await SeedClearingHousesAsync(context);
        await new RegulatoryCatalogSeeder(context).SeedAsync();
        var sut = new AchRegulatoryCatalogService(context);

        var result = await sut.ValidateReturnCodeAsync(ach.Id, "R01", TransactionTypeEnum.Debit, DateTime.UtcNow.Date, DateTime.UtcNow.Date, CancellationToken.None);
        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task AchRegulatoryCatalogService_ShouldRejectAchOnlyCode_ForCenitRail_WhenNotConfigured()
    {
        await using var context = await CreateContextAsync();
        var (cenit, _) = await SeedClearingHousesAsync(context);
        await new RegulatoryCatalogSeeder(context).SeedAsync();
        var sut = new AchRegulatoryCatalogService(context);

        var result = await sut.ValidateReturnCodeAsync(cenit.Id, "DEV14", TransactionTypeEnum.Debit, DateTime.UtcNow.Date, DateTime.UtcNow.Date, CancellationToken.None);
        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task TransactionValidator_ShouldNormalizeKnownReturnReason_CurrentCatalog()
    {
        await using var context = await CreateContextAsync();
        await SeedClearingHousesAsync(context);
        await new RegulatoryCatalogSeeder(context).SeedAsync();
        var sut = new TransactionValidator(context);

        var dto = new AddendaDto { AddendaType = "99", BusinessType = AchAddendaBusinessType.Return, ReturnReasonCode = " dev14 ", OriginalTraceNumber = "123456789012345", NewTraceNumber = "543210987654321" };
        var normalized = sut.NormalizeAndValidateAddenda(dto, TransactionTypeEnum.Return, false, "RETORNO");
        Assert.Equal("DEV14", normalized.ReturnReasonCode);
    }

    [Fact]
    public async Task TransactionValidator_ShouldNotInferRailFromReturnReason()
    {
        await using var context = await CreateContextAsync();
        var (cenit, _) = await SeedClearingHousesAsync(context);
        context.AchReturnCodes.Add(new AchReturnCode { ClearingHouseId = cenit.Id, Code = "R01", Description = "x", AppliesToDebit = true, IsActive = true });
        await context.SaveChangesAsync();

        var sut = new TransactionValidator(context);
        var dto = new AddendaDto { AddendaType = "99", BusinessType = AchAddendaBusinessType.Return, ReturnReasonCode = "R01", OriginalTraceNumber = "123456789012345", NewTraceNumber = "543210987654321" };
        var normalized = sut.NormalizeAndValidateAddenda(dto, TransactionTypeEnum.Return, false, "RETORNO");

        Assert.Equal("R01", normalized.ReturnReasonCode); // comportamiento actual: valida formato/catálogo global, no rail.
    }

    [Fact]
    public async Task CauseCodeCharacterization_ShouldKeepLiquidityInternalCodeOutOfExternalReturnCatalog()
    {
        await using var context = await CreateContextAsync();
        await SeedClearingHousesAsync(context);
        await new RegulatoryCatalogSeeder(context).SeedAsync();

        var returnCodes = await context.AchReturnCodes.Select(x => x.Code).ToListAsync();
        Assert.DoesNotContain("DXX-LIQ", returnCodes);
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

    private static async Task<(ClearingHouse cenit, ClearingHouse ach)> SeedClearingHousesAsync(AchDbContext context)
    {
        var configC = new ClearingHouseConfig { ClearingHouseId = 7001, HolidayStrategy = "Colombian" };
        var configA = new ClearingHouseConfig { ClearingHouseId = 7002, HolidayStrategy = "Colombian" };
        context.ClearingHouseConfigs.AddRange(configC, configA);
        await context.SaveChangesAsync();

        var cenit = new ClearingHouse { Name = "CENIT", Code = "CENIT", OriginCode = "000101006", ClearingHouseId = configC.Id };
        var ach = new ClearingHouse { Name = "ACH Colombia", Code = "ACH", OriginCode = "000101006", ClearingHouseId = configA.Id };
        context.ClearingHouses.AddRange(cenit, ach);
        await context.SaveChangesAsync();
        return (cenit, ach);
    }
}
