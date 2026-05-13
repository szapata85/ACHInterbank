using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class AchRegulatoryCatalogClearingHouseTests
{
    [Fact]
    public async Task ValidateReturnCode_ShouldAllowCode_ForMatchingClearingHouse()
    {
        await using var context = await CreateContextAsync();
        var (cenit, _) = await SeedClearingHousesAsync(context);
        context.AchReturnCodes.Add(new AchReturnCode { ClearingHouseId = cenit, Code = "R01", Description = "ok", FlowType = AchReturnFlowType.Any, EffectiveFrom = DateTime.UtcNow.Date.AddDays(-10), AppliesToDebit = true, IsActive = true });
        await context.SaveChangesAsync();

        var sut = new AchRegulatoryCatalogService(context);
        var result = await sut.ValidateReturnCodeAsync(cenit, "R01", TransactionTypeEnum.Debit, DateTime.UtcNow.Date, DateTime.UtcNow.Date, CancellationToken.None);
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task ValidateReturnCode_ShouldRejectCode_ForDifferentClearingHouse()
    {
        await using var context = await CreateContextAsync();
        var (cenit, ach) = await SeedClearingHousesAsync(context);
        context.AchReturnCodes.Add(new AchReturnCode { ClearingHouseId = cenit, Code = "R01", Description = "ok", FlowType = AchReturnFlowType.Any, EffectiveFrom = DateTime.UtcNow.Date.AddDays(-10), AppliesToDebit = true, IsActive = true });
        await context.SaveChangesAsync();

        var sut = new AchRegulatoryCatalogService(context);
        var result = await sut.ValidateReturnCodeAsync(ach, "R01", TransactionTypeEnum.Debit, DateTime.UtcNow.Date, DateTime.UtcNow.Date, CancellationToken.None);
        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task ValidateReturnPolicy_ShouldUseClearingHouseSpecificPolicy()
    {
        await using var context = await CreateContextAsync();
        var (cenit, ach) = await SeedClearingHousesAsync(context);
        context.AchReturnPolicies.Add(new AchReturnPolicy { ClearingHouseId = cenit, TransactionType = "Debit", AllowedReturnCodesCsv = "R01", Direction = AchReturnDirection.Any, FlowType = AchReturnFlowType.Return, EffectiveFrom = DateTime.UtcNow.Date.AddDays(-10), MaxDays = 5, RequiredOriginalTransactionState = "Pending", IsActive = true });
        context.AchReturnPolicies.Add(new AchReturnPolicy { ClearingHouseId = ach, TransactionType = "Debit", AllowedReturnCodesCsv = "R02", Direction = AchReturnDirection.Any, FlowType = AchReturnFlowType.Return, EffectiveFrom = DateTime.UtcNow.Date.AddDays(-10), MaxDays = 5, RequiredOriginalTransactionState = "Pending", IsActive = true });
        await context.SaveChangesAsync();

        var sut = new AchRegulatoryCatalogService(context);
        var cenitResult = await sut.ValidateReturnPolicyAsync(cenit, TransactionTypeEnum.Debit, "R01", DateTime.UtcNow.Date, DateTime.UtcNow.Date, true, "Pending", CancellationToken.None);
        var achResult = await sut.ValidateReturnPolicyAsync(ach, TransactionTypeEnum.Debit, "R01", DateTime.UtcNow.Date, DateTime.UtcNow.Date, true, "Pending", CancellationToken.None);
        Assert.True(cenitResult.IsAllowed);
        Assert.False(achResult.IsAllowed);
    }

    [Fact]
    public async Task ValidateReturnPolicy_ShouldRespectEffectiveDates()
    {
        await using var context = await CreateContextAsync();
        var (cenit, _) = await SeedClearingHousesAsync(context);
        context.AchReturnPolicies.Add(new AchReturnPolicy { ClearingHouseId = cenit, TransactionType = "Debit", AllowedReturnCodesCsv = "R01", Direction = AchReturnDirection.Any, FlowType = AchReturnFlowType.Return, EffectiveFrom = DateTime.UtcNow.Date.AddDays(1), MaxDays = 5, RequiredOriginalTransactionState = "Pending", IsActive = true });
        await context.SaveChangesAsync();

        var sut = new AchRegulatoryCatalogService(context);
        var result = await sut.ValidateReturnPolicyAsync(cenit, TransactionTypeEnum.Debit, "R01", DateTime.UtcNow.Date, DateTime.UtcNow.Date, true, "Pending", CancellationToken.None);
        Assert.False(result.IsAllowed);
    }

    private static async Task<(int cenit, int ach)> SeedClearingHousesAsync(AchDbContext context)
    {
        var config1 = new ClearingHouseConfig { ClearingHouseId = 1001, HolidayStrategy = "Col" };
        var config2 = new ClearingHouseConfig { ClearingHouseId = 1002, HolidayStrategy = "Col" };
        context.ClearingHouseConfigs.AddRange(config1, config2);
        await context.SaveChangesAsync();
        var c1 = new ClearingHouse { Name = "CENIT", Code = "CENIT", OriginCode = "000101006", ClearingHouseId = config1.Id };
        var c2 = new ClearingHouse { Name = "ACH Colombia", Code = "ACH", OriginCode = "000101007", ClearingHouseId = config2.Id };
        context.ClearingHouses.AddRange(c1, c2);
        await context.SaveChangesAsync();
        return (c1.Id, c2.Id);
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
