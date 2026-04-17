using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IIncomingNachaPrenotificationResolver
{
    Task<IncomingNachaPrenoteResolutionResult> ResolveAsync(
        Guid ingestionId,
        EntryDetail entry,
        int? linkedTransactionId,
        int? resolvedClearingHouseId,
        DateTime? operationalDate,
        string executedBy,
        CancellationToken ct = default);
}
