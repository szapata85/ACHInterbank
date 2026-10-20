using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public sealed class CfaOperationalTransactionRestrictionSeeder(
    AchDbContext context,
    ICycleNumberResolver? cycleNumberResolver = null) : IDbSeeder
{
    // Legacy operational limits had no effective date; keep equivalent always-on bootstrap semantics.
    internal static readonly DateTime BootstrapEffectiveFrom = DateTime.UnixEpoch;
    internal const int HistoricalAchColombiaCycleNumber = 5;

    private readonly ICycleNumberResolver _cycleNumberResolver = cycleNumberResolver ?? new Cfa.ACHInterbank.Application.ACH.Services.CycleNumberResolver();

    int IDbSeeder.Order => 3;

    public async Task SeedAsync()
    {
        var houseRows = await context.ClearingHouses
            .Where(house => house.Code == RegulatoryCycleScheduleCatalog.AchColombiaCode
                || house.Code == RegulatoryCycleScheduleCatalog.CenitCode)
            .Select(house => new { house.Id, house.Code })
            .ToListAsync();

        var achColombia = houseRows.SingleOrDefault(house => house.Code == RegulatoryCycleScheduleCatalog.AchColombiaCode)
            ?? throw new InvalidOperationException(
                $"Seeder CfaOperationalTransactionRestrictionSeeder: falta la cámara '{RegulatoryCycleScheduleCatalog.AchColombiaCode}'.");
        var cenit = houseRows.SingleOrDefault(house => house.Code == RegulatoryCycleScheduleCatalog.CenitCode)
            ?? throw new InvalidOperationException(
                $"Seeder CfaOperationalTransactionRestrictionSeeder: falta la cámara '{RegulatoryCycleScheduleCatalog.CenitCode}'.");

        var historicalAchCycles = await context.ClearingHouseCycleConfigs
            .Where(config => config.ClearingHouseId == achColombia.Id
                && config.PolicyVersion == RegulatoryCycleScheduleCatalog.HistoricalBootstrapPolicyVersion
                && config.EffectiveFrom == RegulatoryCycleScheduleCatalog.HistoricalBootstrapEffectiveFrom)
            .ToListAsync();

        var achCycle5Candidates = historicalAchCycles
            .Where(config => _cycleNumberResolver.Resolve(config.CycleName) == HistoricalAchColombiaCycleNumber)
            .ToList();
        if (achCycle5Candidates.Count != 1)
        {
            throw new InvalidOperationException(
                "Seeder CfaOperationalTransactionRestrictionSeeder: no fue posible resolver de forma única el ciclo histórico 5 de ACH Colombia.");
        }

        var achHistoricalCycle5 = achCycle5Candidates[0];

        await UpsertAsync(new RestrictionSeed(
            ClearingHouseId: null,
            ClearingHouseCycleConfigId: null,
            TransactionType: null,
            IsPrenotification: null,
            MaxAmountPerTransaction: 25_000_000m,
            MaxAmountPerCycle: 250_000_000m,
            MaxTransactionsPerCycle: 500,
            AllowedAccountTypes: [AccountTypeEnum.Checking, AccountTypeEnum.Savings, AccountTypeEnum.ElectronicDeposits]));

        await UpsertAsync(new RestrictionSeed(
            ClearingHouseId: null,
            ClearingHouseCycleConfigId: achHistoricalCycle5.Id,
            TransactionType: TransactionTypeEnum.Debit,
            IsPrenotification: null,
            MaxAmountPerTransaction: null,
            MaxAmountPerCycle: null,
            MaxTransactionsPerCycle: null,
            AllowedAccountTypes: [AccountTypeEnum.Checking, AccountTypeEnum.Savings]));

        await UpsertAsync(new RestrictionSeed(
            ClearingHouseId: cenit.Id,
            ClearingHouseCycleConfigId: null,
            TransactionType: TransactionTypeEnum.Credit,
            IsPrenotification: null,
            MaxAmountPerTransaction: 50_000_000m,
            MaxAmountPerCycle: null,
            MaxTransactionsPerCycle: 800,
            AllowedAccountTypes: [AccountTypeEnum.Checking, AccountTypeEnum.Savings, AccountTypeEnum.ElectronicDeposits]));
    }

    private async Task UpsertAsync(RestrictionSeed seed)
    {
        var restriction = await context.CfaOperationalTransactionRestrictions
            .SingleOrDefaultAsync(item =>
                item.ClearingHouseId == seed.ClearingHouseId
                && item.ClearingHouseCycleConfigId == seed.ClearingHouseCycleConfigId
                && item.TransactionType == seed.TransactionType
                && item.IsPrenotification == seed.IsPrenotification
                && item.EffectiveFrom == BootstrapEffectiveFrom);

        if (restriction is null)
        {
            restriction = new CfaOperationalTransactionRestriction
            {
                ClearingHouseId = seed.ClearingHouseId,
                ClearingHouseCycleConfigId = seed.ClearingHouseCycleConfigId,
                TransactionType = seed.TransactionType,
                IsPrenotification = seed.IsPrenotification,
                EffectiveFrom = BootstrapEffectiveFrom
            };
            context.CfaOperationalTransactionRestrictions.Add(restriction);
        }

        restriction.MaxAmountPerTransaction = seed.MaxAmountPerTransaction;
        restriction.MaxAmountPerCycle = seed.MaxAmountPerCycle;
        restriction.MaxTransactionsPerCycle = seed.MaxTransactionsPerCycle;
        restriction.AllowedAccountTypes = seed.AllowedAccountTypes is null
            ? null
            : [.. seed.AllowedAccountTypes];
        restriction.EffectiveTo = null;
        restriction.IsActive = true;

        await context.SaveChangesAsync();
    }

    private sealed record RestrictionSeed(
        int? ClearingHouseId,
        int? ClearingHouseCycleConfigId,
        TransactionTypeEnum? TransactionType,
        bool? IsPrenotification,
        decimal? MaxAmountPerTransaction,
        decimal? MaxAmountPerCycle,
        int? MaxTransactionsPerCycle,
        IReadOnlyList<AccountTypeEnum>? AllowedAccountTypes);
}
