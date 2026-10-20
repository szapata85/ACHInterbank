using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchStateTransitionService
{
    Task<AchStateTransitionResult> TransitionAsync(
        AchStateTransitionRequest request,
        CancellationToken ct = default);

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
