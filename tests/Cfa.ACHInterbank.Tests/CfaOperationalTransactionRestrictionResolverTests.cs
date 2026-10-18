using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class CfaOperationalTransactionRestrictionResolverTests
{
    [Fact]
    public async Task ResolveAsync_GlobalThenCameraThenCycle_MergesByScope()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 41, 141, "ACHCOL");
        var cycle = AddCycle(context, 41, "V1", 1, 501);
        AddRestriction(context, maxAmountPerTransaction: 10m, maxAmountPerCycle: 100m, maxTransactionsPerCycle: 5);
        AddRestriction(context, clearingHouseId: 41, maxAmountPerTransaction: 20m);
        AddRestriction(context, clearingHouseCycleConfigId: 501, maxAmountPerCycle: 200m);
        await context.SaveChangesAsync();
        var sut = new CfaOperationalTransactionRestrictionResolver(context);

        var result = await sut.ResolveAsync(Request(41, cycle.Id, AccountTypeEnum.Checking));

        Assert.False(result.IsOperationallyDenied);
        Assert.Equal(20m, result.MaxAmountPerTransaction);
        Assert.Equal(200m, result.MaxAmountPerCycle);
        Assert.Equal(5, result.MaxTransactionsPerCycle);
        Assert.Equal(3, result.AppliedRestrictionIds.Count);
    }

    [Fact]
    public async Task ResolveAsync_UndefinedFields_InheritPreviousScope()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 41, 141, "ACHCOL");
        var cycle = AddCycle(context, 41, "V1", 1, 501);
        AddRestriction(
            context,
            maxAmountPerTransaction: 25m,
            maxAmountPerCycle: 250m,
            maxTransactionsPerCycle: 500,
            allowedAccountTypes: [AccountTypeEnum.Checking, AccountTypeEnum.Savings, AccountTypeEnum.ElectronicDeposits]);
        AddRestriction(context, clearingHouseId: 41, maxAmountPerTransaction: 50m);
        AddRestriction(context, clearingHouseCycleConfigId: 501, allowedAccountTypes: [AccountTypeEnum.Checking, AccountTypeEnum.Savings]);
        await context.SaveChangesAsync();
        var sut = new CfaOperationalTransactionRestrictionResolver(context);

        var result = await sut.ResolveAsync(Request(41, cycle.Id, AccountTypeEnum.Checking));

        Assert.Equal(50m, result.MaxAmountPerTransaction);
        Assert.Equal(250m, result.MaxAmountPerCycle);
        Assert.Equal(500, result.MaxTransactionsPerCycle);
        Assert.Equal(new[] { AccountTypeEnum.Checking, AccountTypeEnum.Savings }, result.AllowedAccountTypes);
    }

    [Fact]
    public async Task ResolveAsync_DifferentClearingHouses_RemainIsolated()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 41, 141, "ACHCOL");
        SeedHouse(context, 92, 192, "CENIT");
        AddRestriction(context, maxAmountPerTransaction: 5m);
        AddRestriction(context, clearingHouseId: 41, maxAmountPerTransaction: 41m);
        AddRestriction(context, clearingHouseId: 92, maxAmountPerTransaction: 92m);
        await context.SaveChangesAsync();
        var sut = new CfaOperationalTransactionRestrictionResolver(context);

        var ach = await sut.ResolveAsync(Request(41, null, AccountTypeEnum.Checking));
        var cenit = await sut.ResolveAsync(Request(92, null, AccountTypeEnum.Checking));

        Assert.Equal(41m, ach.MaxAmountPerTransaction);
        Assert.Equal(92m, cenit.MaxAmountPerTransaction);
    }

    [Fact]
    public async Task ResolveAsync_CycleRestriction_IsolatedByPolicyVersionConfigId()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 41, 141, "ACHCOL");
        var v1 = AddCycle(context, 41, "V1", 5, 501);
        var v2 = AddCycle(context, 41, "V2", 5, 502);
        var global = AddRestriction(context, maxAmountPerTransaction: 100m);
        var v1Only = AddRestriction(context, clearingHouseCycleConfigId: 501, maxAmountPerTransaction: 10m);
        await context.SaveChangesAsync();
        var sut = new CfaOperationalTransactionRestrictionResolver(context);

        var historical = await sut.ResolveAsync(Request(41, v1.Id, AccountTypeEnum.Checking));
        var nextVersion = await sut.ResolveAsync(Request(41, v2.Id, AccountTypeEnum.Checking));

        Assert.Equal(10m, historical.MaxAmountPerTransaction);
        Assert.Equal(100m, nextVersion.MaxAmountPerTransaction);
        Assert.Contains(global.Id, historical.AppliedRestrictionIds);
        Assert.Contains(global.Id, nextVersion.AppliedRestrictionIds);
        Assert.Contains(v1Only.Id, historical.AppliedRestrictionIds);
        Assert.DoesNotContain(v1Only.Id, nextVersion.AppliedRestrictionIds);
    }

    [Fact]
    public async Task ResolveAsync_UsesOnlyActiveRulesInsideEffectiveWindow()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 41, 141, "ACHCOL");
        AddRestriction(
            context,
            maxAmountPerTransaction: 10m,
            effectiveFrom: Utc(2026, 1, 1),
            effectiveTo: Utc(2026, 1, 31));
        AddRestriction(
            context,
            maxAmountPerTransaction: 20m,
            effectiveFrom: Utc(2026, 2, 1));
        AddRestriction(
            context,
            clearingHouseId: 41,
            maxAmountPerTransaction: 999m,
            effectiveFrom: Utc(2026, 1, 1),
            isActive: false);
        await context.SaveChangesAsync();
        var sut = new CfaOperationalTransactionRestrictionResolver(context);

        var january = await sut.ResolveAsync(Request(41, null, AccountTypeEnum.Checking, Utc(2026, 1, 31)));
        var february = await sut.ResolveAsync(Request(41, null, AccountTypeEnum.Checking, Utc(2026, 2, 1)));

        Assert.Equal(10m, january.MaxAmountPerTransaction);
        Assert.Equal(20m, february.MaxAmountPerTransaction);
    }

    [Fact]
    public async Task ResolveAsync_MoreSpecificAllowedAccountTypes_IsExplicitVetoWithoutFallback()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 41, 141, "ACHCOL");
        var cycle = AddCycle(context, 41, "V1", 5, 501);
        AddRestriction(
            context,
            allowedAccountTypes: [AccountTypeEnum.Checking, AccountTypeEnum.Savings, AccountTypeEnum.ElectronicDeposits]);
        AddRestriction(
            context,
            clearingHouseCycleConfigId: 501,
            transactionType: TransactionTypeEnum.Debit,
            allowedAccountTypes: [AccountTypeEnum.Checking, AccountTypeEnum.Savings]);
        await context.SaveChangesAsync();
        var sut = new CfaOperationalTransactionRestrictionResolver(context);

        var result = await sut.ResolveAsync(Request(
            41,
            cycle.Id,
            AccountTypeEnum.ElectronicDeposits,
            transactionType: TransactionTypeEnum.Debit));

        Assert.True(result.IsOperationallyDenied);
        Assert.NotNull(result.DenyReason);
        Assert.Equal(new[] { AccountTypeEnum.Checking, AccountTypeEnum.Savings }, result.AllowedAccountTypes);
    }

    [Fact]
    public async Task ResolveAsync_CycleFromAnotherClearingHouse_FailsClosed()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 41, 141, "ACHCOL");
        SeedHouse(context, 92, 192, "CENIT");
        var cenitCycle = AddCycle(context, 92, "V1", 1, 801);
        await context.SaveChangesAsync();
        var sut = new CfaOperationalTransactionRestrictionResolver(context);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ResolveAsync(Request(41, cenitCycle.Id, AccountTypeEnum.Checking)));

        Assert.Contains("no pertenece", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static CfaOperationalTransactionRestrictionRequest Request(
        int clearingHouseId,
        int? cycleConfigId,
        AccountTypeEnum accountType,
        DateTime? effectiveAt = null,
        TransactionTypeEnum transactionType = TransactionTypeEnum.Credit,
        bool isPrenotification = false)
        => new(
            clearingHouseId,
            cycleConfigId,
            transactionType,
            isPrenotification,
            accountType,
            effectiveAt ?? Utc(2026, 6, 1));

    private static CfaOperationalTransactionRestriction AddRestriction(
        AchDbContext context,
        int? clearingHouseId = null,
        int? clearingHouseCycleConfigId = null,
        TransactionTypeEnum? transactionType = null,
        bool? isPrenotification = null,
        decimal? maxAmountPerTransaction = null,
        decimal? maxAmountPerCycle = null,
        int? maxTransactionsPerCycle = null,
        List<AccountTypeEnum>? allowedAccountTypes = null,
        DateTime? effectiveFrom = null,
        DateTime? effectiveTo = null,
        bool isActive = true)
    {
        var restriction = new CfaOperationalTransactionRestriction
        {
            ClearingHouseId = clearingHouseId,
            ClearingHouseCycleConfigId = clearingHouseCycleConfigId,
            TransactionType = transactionType,
            IsPrenotification = isPrenotification,
            MaxAmountPerTransaction = maxAmountPerTransaction,
            MaxAmountPerCycle = maxAmountPerCycle,
            MaxTransactionsPerCycle = maxTransactionsPerCycle,
            AllowedAccountTypes = allowedAccountTypes,
            EffectiveFrom = effectiveFrom ?? Utc(2026, 1, 1),
            EffectiveTo = effectiveTo,
            IsActive = isActive
        };
        context.CfaOperationalTransactionRestrictions.Add(restriction);
        return restriction;
    }

    private static ClearingHouseCycleConfig AddCycle(
        AchDbContext context,
        int clearingHouseId,
        string policyVersion,
        int cycleNumber,
        int id)
    {
        var cycle = new ClearingHouseCycleConfig
        {
            Id = id,
            ClearingHouseId = clearingHouseId,
            PolicyVersion = policyVersion,
            CycleName = $"Ciclo {cycleNumber}",
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(9, 0, 0),
            CutoffTime = new TimeSpan(9, 0, 0),
            OutputReleaseTime = new TimeSpan(10, 0, 0),
            EffectiveFrom = Utc(2026, 1, 1),
            IsActive = true
        };
        context.ClearingHouseCycleConfigs.Add(cycle);
        return cycle;
    }

    private static void SeedHouse(AchDbContext context, int houseId, int configId, string code)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = configId,
            ClearingHouseId = houseId,
            TimeZoneId = "America/Bogota",
            HolidayStrategy = "Colombian"
        });
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = houseId,
            ClearingHouseId = configId,
            Code = code,
            Name = code,
            OriginCode = $"{houseId:000000000}",
            IsActive = true
        });
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static AchDbContext CreateContext(SqliteConnection connection)
    {
        var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options);
        context.Database.EnsureCreated();
        return context;
    }

    private static DateTime Utc(int year, int month, int day)
        => DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc);
}
