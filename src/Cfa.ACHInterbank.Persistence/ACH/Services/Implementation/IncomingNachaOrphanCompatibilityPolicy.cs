using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal static class IncomingNachaOrphanCompatibilityPolicy
{
    public static IReadOnlyList<string> Evaluate(
        IncomingNachaFileIngestion ingestion,
        EntryDetail entry,
        IncomingNachaEntryClassification classification,
        AchTransaction transaction,
        IReadOnlyCollection<int> candidateTransactionIds)
    {
        var reasons = new List<string>();

        if (ingestion.ResolvedClearingHouseId.HasValue
            && transaction.AchCycle?.ClearingHouseId != ingestion.ResolvedClearingHouseId.Value)
        {
            reasons.Add("La transacción pertenece a otra cámara compensadora.");
        }

        if (entry.Amount.GetValueOrDefault() != transaction.Amount)
        {
            reasons.Add("El valor no coincide con la devolución recibida.");
        }

        var account = entry.AccountNumber?.Trim();
        if (!string.IsNullOrWhiteSpace(account)
            && !string.Equals(account, transaction.DestinationAccountNumber?.Trim(), StringComparison.Ordinal))
        {
            reasons.Add("La cuenta receptora no coincide con la transacción original.");
        }

        var originalTrace = classification.OriginalTraceRef?.Trim();
        if (!string.IsNullOrWhiteSpace(originalTrace)
            && !string.Equals(originalTrace, transaction.TraceNumber?.Trim(), StringComparison.Ordinal)
            && !string.Equals(originalTrace, transaction.OriginalTraceRef?.Trim(), StringComparison.Ordinal))
        {
            reasons.Add("El número de rastreo original no coincide.");
        }

        if (candidateTransactionIds.Count > 0 && !candidateTransactionIds.Contains(transaction.Id))
        {
            reasons.Add("La transacción no pertenece al conjunto ambiguo conservado durante la correlación automática.");
        }

        return reasons;
    }
}
