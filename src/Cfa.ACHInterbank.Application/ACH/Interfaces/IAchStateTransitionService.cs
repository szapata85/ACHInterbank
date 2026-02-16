using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchStateTransitionService
{
    Task<AchTransaction> TransitionAsync(
        int transactionId,
        AchTransferStateEnum toState,
        AchStateEventSourceEnum source,
        string? reasonCode = null,
        string? payloadJson = null,
        string? originalTraceRef = null,
        DateTime? changedAtUtc = null,
        CancellationToken ct = default);
}
