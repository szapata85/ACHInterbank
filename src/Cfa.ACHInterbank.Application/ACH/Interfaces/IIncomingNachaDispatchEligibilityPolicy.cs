using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IIncomingNachaDispatchEligibilityPolicy
{
    Task<IncomingNachaDispatchEligibilityResult> EvaluateAsync(
        IncomingNachaFileIngestion ingestion,
        IncomingNachaEntryClassification classification,
        IncomingNachaTransactionLink link,
        AchTransaction transaction,
        DateTime nowLocal,
        CancellationToken ct = default);

    /// <summary>
    /// ACH Colombia file-based eligibility: never infer an internal AchCycle or
    /// require an AchTransaction/legacy link. The result is not permission to dispatch.
    /// </summary>
    Task<IncomingNachaDispatchEligibilityResult> EvaluateFileBasedAsync(
        IncomingNachaFileIngestion ingestion,
        IncomingNachaEntryClassification classification,
        NachaHeader header,
        EntryDetail entry,
        ClearingHouse clearingHouse,
        bool featureEnabled,
        CancellationToken ct = default);
}
