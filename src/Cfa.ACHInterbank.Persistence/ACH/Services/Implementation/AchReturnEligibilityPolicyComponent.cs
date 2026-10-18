using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal sealed class AchReturnEligibilityPolicyComponent(
    AchDbContext context,
    IAchReturnEligibilityService returnEligibilityService,
    ICenitIncomingReturnPolicy? cenitReturnPolicy)
{
    public void ValidateScopeRequest(
        AchReturnGenerationScope scope,
        GenerateReturnsFileRequest request)
    {
        if (scope.IsAchColombia && request.Items.Any(item =>
                string.Equals(item.ReturnReasonCode?.Trim(), "DEV14", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("ACHCOL_DEV14_CLAIMS_ONLY: DEV14 pertenece al flujo de reclamaciones y no puede generar ReturnOut.");
        }

        if (scope.IsCenit && cenitReturnPolicy is null)
        {
            throw new InvalidOperationException("CENIT_RETURN_POLICY_REQUIRED: la policy normativa CENIT no está registrada.");
        }
    }

    public async Task ValidatePreEligibilityAsync(
        AchTransaction transaction,
        AchReturnGenerationScope scope,
        ReturnSelectionItemDto item,
        CancellationToken ct)
    {
        if (transaction.Type is TransactionTypeEnum.Return or TransactionTypeEnum.Reversal)
        {
            throw new InvalidOperationException($"La transacción {transaction.Id} no es elegible para devolución porque ya corresponde a un retorno o reverso.");
        }

        if (scope.IsCenit && CenitReturnIn2026Layout.IsReturnOfReturnCause(item.ReturnReasonCode))
        {
            throw new InvalidOperationException("CENIT_ROR_NOT_ORDINARY_RETURN: las causales R60-R74 requieren el flujo independiente de devolución de una devolución.");
        }

        if (scope.IsAchColombia && string.Equals(item.ReturnReasonCode?.Trim(), "R10", StringComparison.OrdinalIgnoreCase))
        {
            await ValidateAchColombiaR10Async(transaction, scope.ReturnCycle, item.AchColombiaR10Basis, ct);
        }
    }

    public async Task<string> ResolveNormalizedReasonAsync(
        AchTransaction transaction,
        AchReturnGenerationScope scope,
        ReturnSelectionItemDto item,
        DateTime now,
        CancellationToken ct)
    {
        var eligibility = await returnEligibilityService.EvaluateOutgoingReturnAsync(
            new AchReturnEligibilityRequest(
                transaction.Id,
                item.ReturnReasonCode ?? string.Empty,
                scope.IsCenit ? scope.ReturnCycle.ProcessingDate : now,
                HasAddenda: true),
            ct);
        if (!eligibility.IsEligible)
        {
            throw new InvalidOperationException(eligibility.Failures.First().Message);
        }

        var reasonCode = eligibility.NormalizedReasonCode!;
        if (scope.IsCenit && CenitReturnIn2026Layout.IsReturnOfReturnCause(reasonCode))
        {
            throw new InvalidOperationException("CENIT_ROR_NOT_ORDINARY_RETURN: las causales R60-R74 requieren el flujo independiente de devolución de una devolución.");
        }

        return reasonCode;
    }

    public void ValidateCenitOperationalPolicy(
        AchTransaction transaction,
        AchReturnGenerationScope scope,
        AchReturnCorrelationResult correlation,
        ReturnSelectionItemDto item,
        string reasonCode,
        decimal amount)
    {
        if (!scope.IsCenit)
        {
            return;
        }

        var evidence = item.CenitOperationalEvidence ?? new CenitIncomingReturnOperationalEvidence();
        var prenoteDirection = evidence.PrenotificationDirection != CenitPrenotificationDirection.Unknown
            ? evidence.PrenotificationDirection
            : ResolveCenitPrenotificationDirection(transaction.TransactionCode);
        var cycleEvidence = correlation.CenitCycleEvidence!;
        var policyResult = cenitReturnPolicy!.Evaluate(new CenitIncomingReturnPolicyRequest(
            transaction.Type,
            reasonCode,
            transaction.EffectiveEntryDate,
            scope.ReturnCycle.ProcessingDate,
            cycleEvidence.OriginalCycleNumber,
            cycleEvidence.ReturnCycleNumber,
            cycleEvidence.LastReturnCycleNumber,
            transaction.Amount,
            amount,
            prenoteDirection,
            evidence.ReturnRequestDate,
            evidence.ImmediateReturnCycleConfirmed,
            evidence.FundsAvailabilityRequired,
            evidence.FundsAvailabilityConfirmed,
            evidence.ConfirmationToOriginatorRecorded,
            evidence.ReceiverRejectionDeadlineDate));
        if (!policyResult.IsAllowed)
        {
            throw new InvalidOperationException($"{policyResult.Code}: {policyResult.Message}");
        }
    }

    private async Task ValidateAchColombiaR10Async(
        AchTransaction transaction,
        AchCycle returnCycle,
        AchColombiaR10ReturnBasis? requestedBasis,
        CancellationToken ct)
    {
        if (transaction.Type != TransactionTypeEnum.Debit || transaction.IsPrenotification)
        {
            throw new InvalidOperationException("ACHCOL_R10_DEBIT_REQUIRED: R10 solo aplica a un débito monetario elegible.");
        }

        var originalCycle = transaction.AchCycle
            ?? throw new InvalidOperationException("ACHCOL_R10_ORIGINAL_CYCLE_REQUIRED: no fue posible resolver el ciclo original.");
        if (originalCycle.ProcessingDate.Date != returnCycle.ProcessingDate.Date)
        {
            throw new InvalidOperationException("ACHCOL_RETURN_PREVIOUS_OPERATIONAL_DAY: la devolución R10 fuera del día operativo pertenece al flujo de reclamaciones.");
        }

        _ = originalCycle.ClearingHouseCycleConfig
            ?? throw new InvalidOperationException("ACHCOL_R10_WINDOW_POLICY_UNRESOLVED: el ciclo original no conserva su política de ciclos.");
        var maxReturnCycles = await ResolveAchColombiaMaxReturnCyclesAsync(
            originalCycle.ClearingHouseId,
            originalCycle.ProcessingDate,
            ct);

        var scheduledCycles = await context.AchCycles
            .AsNoTracking()
            .Include(cycle => cycle.ClearingHouseCycleConfig)
            .Where(cycle => cycle.ClearingHouseId == originalCycle.ClearingHouseId
                && cycle.ProcessingDate.Date == originalCycle.ProcessingDate.Date)
            .OrderBy(cycle => cycle.StartTime)
            .ThenBy(cycle => cycle.CutoffTime)
            .ToListAsync(ct);
        var originalIndex = scheduledCycles.FindIndex(cycle => cycle.Id == originalCycle.Id);
        var returnIndex = scheduledCycles.FindIndex(cycle => cycle.Id == returnCycle.Id);
        if (originalIndex < 0 || returnIndex < originalIndex)
        {
            throw new InvalidOperationException("ACHCOL_R10_WINDOW_POLICY_UNRESOLVED: no fue posible resolver el orden operativo de los ciclos.");
        }

        var returnOpportunityCount = scheduledCycles
            .Skip(originalIndex)
            .Take(returnIndex - originalIndex + 1)
            .Count(cycle => cycle.ClearingHouseCycleConfig?.AllowsReturn == true);
        if (scheduledCycles[returnIndex].ClearingHouseCycleConfig?.AllowsReturn != true)
        {
            throw new InvalidOperationException("ACHCOL_R10_RETURN_NOT_ALLOWED_IN_CYCLE: la política del ciclo solicitado no permite ReturnOut.");
        }
        if (returnOpportunityCount > maxReturnCycles)
        {
            throw new InvalidOperationException("ACHCOL_R10_RETURN_WINDOW_EXPIRED: la transacción excede la ventana configurada de devolución.");
        }

        if (requestedBasis == AchColombiaR10ReturnBasis.NoReceiverAuthorizationOrAgreement)
        {
            return;
        }

        var prenotificationCode = NachaTransactionCodeTaxonomy.ResolvePrenotificationCode(transaction.TransactionCode);
        if (string.IsNullOrWhiteSpace(prenotificationCode)
            || transaction.DestinationInstitutionId <= 0
            || string.IsNullOrWhiteSpace(transaction.DestinationAccountNumber))
        {
            throw new InvalidOperationException("ACHCOL_R10_BASIS_UNPROVEN: no se puede probar la ausencia de prenotificación con la información original.");
        }

        var prenotificationExists = await context.AchTransactions
            .AsNoTracking()
            .AnyAsync(candidate => candidate.Id != transaction.Id
                && candidate.IsPrenotification
                && candidate.DestinationInstitutionId == transaction.DestinationInstitutionId
                && candidate.DestinationAccountNumber == transaction.DestinationAccountNumber
                && candidate.TransactionCode == prenotificationCode
                && candidate.EffectiveEntryDate.Date <= transaction.EffectiveEntryDate.Date, ct);
        if (prenotificationExists)
        {
            var code = requestedBasis == AchColombiaR10ReturnBasis.NoPrenotification
                ? "ACHCOL_R10_BASIS_CONTRADICTED"
                : "ACHCOL_R10_BASIS_UNPROVEN";
            throw new InvalidOperationException($"{code}: existe evidencia de prenotificación; seleccione una base R10 válida y verificable.");
        }

        if (requestedBasis is null or AchColombiaR10ReturnBasis.NoPrenotification)
        {
            return;
        }

        throw new InvalidOperationException("ACHCOL_R10_BASIS_UNPROVEN: la base R10 no es reconocida.");
    }

    private async Task<int> ResolveAchColombiaMaxReturnCyclesAsync(
        int clearingHouseId,
        DateTime operationalDate,
        CancellationToken ct)
    {
        var policies = await context.AchReturnPolicies
            .AsNoTracking()
            .Where(policy => policy.ClearingHouseId == clearingHouseId
                && policy.IsActive
                && policy.TransactionType == "Debit"
                && policy.Direction == AchReturnDirection.Any
                && policy.FlowType == AchReturnFlowType.Return
                && policy.EffectiveFrom.Date <= operationalDate.Date
                && (!policy.EffectiveTo.HasValue || policy.EffectiveTo.GetValueOrDefault().Date >= operationalDate.Date))
            .Select(policy => policy.MaxCycles)
            .ToListAsync(ct);

        if (policies.Count != 1 || policies[0] is not int maxReturnCycles || maxReturnCycles <= 0)
        {
            throw new InvalidOperationException("ACHCOL_R10_WINDOW_POLICY_UNRESOLVED: la política efectiva de devolución no define un máximo de ciclos único.");
        }

        return maxReturnCycles;
    }

    private static CenitPrenotificationDirection ResolveCenitPrenotificationDirection(string transactionCode)
        => transactionCode switch
        {
            "21" or "22" or "23" or "31" or "32" or "33" or "51" or "52" or "53" => CenitPrenotificationDirection.Credit,
            "26" or "27" or "28" or "36" or "37" or "38" or "55" or "56" or "57" => CenitPrenotificationDirection.Debit,
            _ => CenitPrenotificationDirection.Unknown
        };
}
