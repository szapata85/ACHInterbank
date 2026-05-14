using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public class AchReturnEligibilityService(
    AchDbContext context,
    IAchRegulatoryCatalogService regulatoryCatalogService) : IAchReturnEligibilityService
{
    public async Task<AchReturnEligibilityResult> EvaluateOutgoingReturnAsync(AchReturnEligibilityRequest request, CancellationToken cancellationToken)
    {
        var failures = new List<AchReturnEligibilityFailure>();
        var tx = await context.AchTransactions
            .AsNoTracking()
            .Include(t => t.AchCycle)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

        if (tx is null)
        {
            failures.Add(new("TRANSACTION_NOT_FOUND", "No existe la transacción seleccionada.", nameof(request.TransactionId)));
            return new(false, null, null, null, null, failures);
        }

        var clearingHouseId = tx.AchCycle?.ClearingHouseId;
        if (!clearingHouseId.HasValue || clearingHouseId.Value <= 0)
        {
            failures.Add(new("CLEARING_HOUSE_MISSING", "La transacción no tiene cámara de compensación válida.", "ClearingHouseId"));
            return new(false, null, null, tx.Type.ToString(), tx.State.ToString(), failures);
        }


        if (tx.State is Domain.Entities.Transactions.Enums.AchTransferStateEnum.ReturnedByOperator or Domain.Entities.Transactions.Enums.AchTransferStateEnum.ReturnedByEpr)
        {
            failures.Add(new("RETURN_ALREADY_PROCESSED", "La transacción ya fue devuelta o ya tiene una devolución procesada.", nameof(request.TransactionId)));
            return new(false, null, clearingHouseId, tx.Type.ToString(), tx.State.ToString(), failures);
        }

        var alreadyIncludedInReturnFile = await context.Set<AchReturnGenerated>()
            .AsNoTracking()
            .AnyAsync(x => x.OriginalTransactionId == tx.Id, cancellationToken);
        if (alreadyIncludedInReturnFile)
        {
            failures.Add(new("RETURN_ALREADY_INCLUDED_IN_FILE", "La transacción ya fue devuelta o ya tiene una devolución procesada.", nameof(request.TransactionId)));
            return new(false, null, clearingHouseId, tx.Type.ToString(), tx.State.ToString(), failures);
        }
        if (string.IsNullOrWhiteSpace(request.ReturnReasonCode))
        {
            failures.Add(new("RETURN_REASON_REQUIRED", "La causal de devolución es obligatoria.", nameof(request.ReturnReasonCode)));
            return new(false, null, clearingHouseId, tx.Type.ToString(), tx.State.ToString(), failures);
        }

        if (request.ReturnDate.Date < tx.EffectiveEntryDate.Date)
        {
            failures.Add(new("RETURN_DATE_BEFORE_ORIGINAL", "La fecha de devolución no puede ser anterior a la fecha efectiva original.", nameof(request.ReturnDate)));
            return new(false, null, clearingHouseId, tx.Type.ToString(), tx.State.ToString(), failures);
        }

        var normalizedReasonCode = request.ReturnReasonCode.Trim().ToUpperInvariant();
        var returnCodeValidation = await regulatoryCatalogService.ValidateReturnCodeAsync(
            clearingHouseId.Value,
            normalizedReasonCode,
            tx.Type,
            tx.EffectiveEntryDate.Date,
            request.ReturnDate.Date,
            cancellationToken);

        if (!returnCodeValidation.IsAllowed)
        {
            failures.Add(new(
                "RETURN_CODE_REJECTED",
                returnCodeValidation.Reason ?? $"La causal {normalizedReasonCode} no está permitida para la transacción.",
                nameof(request.ReturnReasonCode)));
            return new(false, normalizedReasonCode, clearingHouseId, tx.Type.ToString(), tx.State.ToString(), failures);
        }

        var returnPolicyValidation = await regulatoryCatalogService.ValidateReturnPolicyAsync(
            clearingHouseId.Value,
            tx.Type,
            normalizedReasonCode,
            tx.EffectiveEntryDate.Date,
            request.ReturnDate.Date,
            request.HasAddenda,
            tx.State.ToString(),
            cancellationToken);

        if (!returnPolicyValidation.IsAllowed)
        {
            failures.Add(new("RETURN_POLICY_REJECTED", returnPolicyValidation.Reason ?? "La política regulatoria no permite la devolución."));
        }

        return new(failures.Count == 0, normalizedReasonCode, clearingHouseId, tx.Type.ToString(), tx.State.ToString(), failures);
    }
}
