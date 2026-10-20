using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class CfaOperationalTransactionRestrictionSeederTests
{
    [Fact]
    public async Task SeedAsync_IsIdempotent_AndResolvesStableCodesWithoutHardcodedIds()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        await using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options);
        context.Database.EnsureCreated();

        SeedHouse(context, 41, 141, RegulatoryCycleScheduleCatalog.AchColombiaCode);
        SeedHouse(context, 92, 192, RegulatoryCycleScheduleCatalog.CenitCode);
        var historicalCycles = RegulatoryCycleScheduleCatalog.AchColombiaV35HistoricalPolicy.Cycles
            .Select((schedule, index) => new ClearingHouseCycleConfig
            {
                Id = 501 + index,
                ClearingHouseId = 41,
                PolicyVersion = RegulatoryCycleScheduleCatalog.HistoricalBootstrapPolicyVersion,
                CycleName = $"Ciclo {schedule.CycleNumber}",
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                CutoffTime = schedule.CutoffTime,
                OutputReleaseTime = schedule.OutputReleaseTime,
                AllowsMonetaryCredit = schedule.AllowsMonetaryCredit,
                AllowsMonetaryDebit = schedule.AllowsMonetaryDebit,
                AllowsCreditPrenotification = schedule.AllowsCreditPrenotification,
                AllowsDebitPrenotification = schedule.AllowsDebitPrenotification,
                AllowsReturn = schedule.AllowsReturn,
                AllowsReturnOfReturn = schedule.AllowsReturnOfReturn,
                EffectiveFrom = RegulatoryCycleScheduleCatalog.HistoricalBootstrapEffectiveFrom,
                IsActive = true
            })
            .ToArray();
        context.ClearingHouseCycleConfigs.AddRange(historicalCycles);
        await context.SaveChangesAsync();

        var seeder = new CfaOperationalTransactionRestrictionSeeder(context, new CycleNumberResolver());
        await seeder.SeedAsync();
        var first = await context.CfaOperationalTransactionRestrictions
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .ToListAsync();
        await seeder.SeedAsync();
        var second = await context.CfaOperationalTransactionRestrictions
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .ToListAsync();

        Assert.Equal(3, first.Count);
        Assert.Equal(first.Select(item => item.Id), second.Select(item => item.Id));
        Assert.Equal(first.Select(item => Convert.ToHexString(item.RowVersion)), second.Select(item => Convert.ToHexString(item.RowVersion)));
        Assert.Equal(first.Select(item => item.UpdatedAt), second.Select(item => item.UpdatedAt));
        Assert.Equal(3, second.Count);

        var global = second.Single(item => item.ClearingHouseId is null && item.ClearingHouseCycleConfigId is null);
        Assert.Equal(25_000_000m, global.MaxAmountPerTransaction);
        Assert.Equal(250_000_000m, global.MaxAmountPerCycle);
        Assert.Equal(500, global.MaxTransactionsPerCycle);
        Assert.Equal(
            new[] { AccountTypeEnum.Checking, AccountTypeEnum.Savings, AccountTypeEnum.ElectronicDeposits },
            global.AllowedAccountTypes);

        var cycle5ConfigId = historicalCycles.Single(cycle => new CycleNumberResolver().Resolve(cycle.CycleName) == 5).Id;
        var achCycle5Debit = second.Single(item => item.ClearingHouseCycleConfigId == cycle5ConfigId);
        Assert.Null(achCycle5Debit.ClearingHouseId);
        Assert.Equal(TransactionTypeEnum.Debit, achCycle5Debit.TransactionType);
        Assert.Equal(new[] { AccountTypeEnum.Checking, AccountTypeEnum.Savings }, achCycle5Debit.AllowedAccountTypes);

        var cenitCredit = second.Single(item => item.ClearingHouseId == 92);
        Assert.Equal(TransactionTypeEnum.Credit, cenitCredit.TransactionType);
        Assert.Equal(50_000_000m, cenitCredit.MaxAmountPerTransaction);
        Assert.Equal(800, cenitCredit.MaxTransactionsPerCycle);

        Assert.DoesNotContain(second, item => item.ClearingHouseId == 1 || item.ClearingHouseId == 2);
        Assert.DoesNotContain(second, item => item.ClearingHouseCycleConfigId == 1 || item.ClearingHouseCycleConfigId == 2);
    }

    [Fact]
    public async Task Model_UsesAuditingAndOptimisticConcurrencyPatterns()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        await using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options);
        context.Database.EnsureCreated();

        var entity = context.Model.FindEntityType(typeof(CfaOperationalTransactionRestriction));
        Assert.NotNull(entity);
        Assert.True(entity!.FindProperty(nameof(CfaOperationalTransactionRestriction.RowVersion))!.IsConcurrencyToken);
        Assert.NotNull(entity.FindProperty(nameof(CfaOperationalTransactionRestriction.CreatedAt)));
        Assert.NotNull(entity.FindProperty(nameof(CfaOperationalTransactionRestriction.UpdatedAt)));

        var restriction = new CfaOperationalTransactionRestriction
        {
            MaxTransactionsPerCycle = 1,
            EffectiveFrom = DateTime.UnixEpoch,
            IsActive = true
        };
        context.CfaOperationalTransactionRestrictions.Add(restriction);
        await context.SaveChangesAsync();

        Assert.NotEqual(default, restriction.CreatedAt);
        Assert.NotEqual(default, restriction.UpdatedAt);
        Assert.Equal(16, restriction.RowVersion.Length);
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
}
