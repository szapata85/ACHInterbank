using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public class AchReturnOfReturnEligibilityService(
    AchDbContext context,
    IAchRegulatoryCatalogService regulatoryCatalogService) : IAchReturnOfReturnEligibilityService
{
    public async Task<AchReturnOfReturnEligibilityResult> EvaluateAsync(AchReturnOfReturnEligibilityRequest request, CancellationToken cancellationToken)
    {
        var failures = new List<AchReturnOfReturnEligibilityFailure>();

        var sourceReturn = await context.AchTransactions
            .AsNoTracking()
            .Include(x => x.AchCycle)
            .FirstOrDefaultAsync(x => x.Id == request.SourceReturnTransactionId, cancellationToken);

        if (sourceReturn is null || sourceReturn.Type != TransactionTypeEnum.Return)
        {
            failures.Add(new("SOURCE_RETURN_NOT_FOUND", "No existe la devolución origen para evaluar devolución de devolución.", nameof(request.SourceReturnTransactionId)));
            return new(false, null, request.SourceReturnTransactionId, null, null, false, failures);
        }

        var clearingHouseId = sourceReturn.AchCycle?.ClearingHouseId;
        if (!clearingHouseId.HasValue || clearingHouseId.Value <= 0)
        {
            clearingHouseId = await context.AchCycles
                .AsNoTracking()
                .Where(x => x.Id == sourceReturn.AchCycleId)
                .Select(x => (int?)x.ClearingHouseId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (!clearingHouseId.HasValue || clearingHouseId.Value <= 0)
        {
            failures.Add(new("CLEARING_HOUSE_MISSING", "No se pudo resolver la cámara de compensación de la devolución origen.", "ClearingHouseId"));
            return new(false, null, sourceReturn.Id, sourceReturn.ReturnReasonCode, null, false, failures);
        }

        var originalReasonCode = sourceReturn.ReturnReasonCode?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(originalReasonCode))
        {
            failures.Add(new("ORIGINAL_RETURN_REASON_MISSING", "La devolución origen no tiene causal original.", nameof(sourceReturn.ReturnReasonCode)));
            return new(false, clearingHouseId, sourceReturn.Id, null, null, false, failures);
        }

        var newReasonCode = request.NewReturnReasonCode?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(newReasonCode))
        {
            failures.Add(new("NEW_RETURN_REASON_REQUIRED", "La nueva causal de devolución es obligatoria.", nameof(request.NewReturnReasonCode)));
            return new(false, clearingHouseId, sourceReturn.Id, originalReasonCode, null, false, failures);
        }

        var validation = await regulatoryCatalogService.ValidateReturnOfReturnAsync(
            clearingHouseId.Value,
            originalReasonCode,
            newReasonCode,
            sourceReturn.State.ToString(),
            sourceReturn.EffectiveEntryDate.Date,
            request.RequestedAtUtc.Date,
            cancellationToken);

        if (!validation.IsAllowed)
        {
            failures.Add(new("RETURN_OF_RETURN_POLICY_REJECTED", validation.Reason ?? "La política regulatoria rechazó la devolución de devolución.", nameof(request.NewReturnReasonCode)));
        }

        return new(failures.Count == 0, clearingHouseId, sourceReturn.Id, originalReasonCode, newReasonCode, validation.IsUniquePerTransaction, failures);
    }
}
