using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record AchStateTransitionRequest(
    int TransactionId,
    AchTransferStateEnum ToState,
    AchStateEventSourceEnum Source,
    string? ReasonCode = null,
    string? PayloadJson = null,
    string? OriginalTraceRef = null,
    DateTime? ChangedAtUtc = null,
    string? IdempotencyKey = null,
    int? ClearingHouseId = null,
    int? AchReturnCodeId = null,
    string? ResolvedReasonDescription = null);

public sealed record AchStateTransitionResult(
    AchTransaction Transaction,
    bool Applied,
    bool WasDuplicate);
