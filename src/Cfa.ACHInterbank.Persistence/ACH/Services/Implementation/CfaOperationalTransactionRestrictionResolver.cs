using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class CfaOperationalTransactionRestrictionResolver(AchDbContext context)
    : ICfaOperationalTransactionRestrictionResolver
{
    public async Task<CfaOperationalTransactionRestrictionResolution> ResolveAsync(
        CfaOperationalTransactionRestrictionRequest request,
        CancellationToken ct = default)
    {
        if (request.ClearingHouseId <= 0)
        {
            throw new InvalidOperationException("La cámara es requerida para resolver restricciones operacionales CFA.");
        }

        if (request.EffectiveAt == default)
        {
            throw new InvalidOperationException("La fecha de vigencia es requerida para resolver restricciones operacionales CFA.");
        }

        if (request.ClearingHouseCycleConfigId.HasValue)
        {
            var cycleHouseId = await context.ClearingHouseCycleConfigs
                .AsNoTracking()
                .Where(config => config.Id == request.ClearingHouseCycleConfigId.Value)
                .Select(config => (int?)config.ClearingHouseId)
                .SingleOrDefaultAsync(ct);

            if (!cycleHouseId.HasValue)
            {
                throw new InvalidOperationException("La configuración versionada del ciclo no existe.");
            }

            if (cycleHouseId.Value != request.ClearingHouseId)
            {
                throw new InvalidOperationException("La configuración versionada del ciclo no pertenece a la cámara indicada.");
            }
        }

        var effectiveAt = request.EffectiveAt;
        var candidates = await context.CfaOperationalTransactionRestrictions
            .AsNoTracking()
            .Where(rule => rule.IsActive
                && rule.EffectiveFrom <= effectiveAt
                && (!rule.EffectiveTo.HasValue || rule.EffectiveTo.Value >= effectiveAt))
            .Where(rule => !rule.TransactionType.HasValue || rule.TransactionType == request.TransactionType)
            .Where(rule => !rule.IsPrenotification.HasValue || rule.IsPrenotification == request.IsPrenotification)
            .Where(rule =>
                (rule.ClearingHouseId == null && rule.ClearingHouseCycleConfigId == null)
                || (rule.ClearingHouseId == request.ClearingHouseId && rule.ClearingHouseCycleConfigId == null)
                || (request.ClearingHouseCycleConfigId.HasValue
                    && rule.ClearingHouseId == null
                    && rule.ClearingHouseCycleConfigId == request.ClearingHouseCycleConfigId.Value))
            .ToListAsync(ct);

        var ordered = candidates
            .OrderBy(ScopeSpecificity)
            .ThenBy(rule => rule.TransactionType.HasValue ? 1 : 0)
            .ThenBy(rule => rule.IsPrenotification.HasValue ? 1 : 0)
            .ThenBy(rule => rule.EffectiveFrom)
            .ThenBy(rule => rule.Id)
            .ToList();

        decimal? maxAmountPerTransaction = null;
        decimal? maxAmountPerCycle = null;
        int? maxTransactionsPerCycle = null;
        List<AccountTypeEnum>? allowedAccountTypes = null;
        var appliedIds = new List<int>(ordered.Count);

        foreach (var rule in ordered)
        {
            appliedIds.Add(rule.Id);

            if (rule.MaxAmountPerTransaction.HasValue)
            {
                maxAmountPerTransaction = rule.MaxAmountPerTransaction;
            }

            if (rule.MaxAmountPerCycle.HasValue)
            {
                maxAmountPerCycle = rule.MaxAmountPerCycle;
            }

            if (rule.MaxTransactionsPerCycle.HasValue)
            {
                maxTransactionsPerCycle = rule.MaxTransactionsPerCycle;
            }

            if (rule.AllowedAccountTypes is not null)
            {
                allowedAccountTypes = rule.AllowedAccountTypes
                    .Distinct()
                    .OrderBy(item => (int)item)
                    .ToList();
            }
        }

        var deniedByAccountType = allowedAccountTypes is not null
            && !allowedAccountTypes.Contains(request.AccountType);

        return new CfaOperationalTransactionRestrictionResolution(
            deniedByAccountType,
            deniedByAccountType
                ? $"El tipo de producto {request.AccountType} está bloqueado por una restricción operacional CFA."
                : null,
            maxAmountPerTransaction,
            maxAmountPerCycle,
            maxTransactionsPerCycle,
            allowedAccountTypes?.AsReadOnly(),
            appliedIds.AsReadOnly());
    }

    private static int ScopeSpecificity(CfaOperationalTransactionRestriction rule)
        => rule.ClearingHouseCycleConfigId.HasValue
            ? 2
            : rule.ClearingHouseId.HasValue
                ? 1
                : 0;
}
