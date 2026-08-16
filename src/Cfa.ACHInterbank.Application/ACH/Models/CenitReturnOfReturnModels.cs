namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record CenitReturnOfReturnOutRequest(
    long ParentIncomingReturnStateEventId,
    string ReasonCode,
    string ReturnCycleId,
    DateTime RequestedAtUtc,
    string? RequestedBy = null,
    string? Source = null);

public sealed record CenitReturnOfReturnInRequest(
    int ParentOutgoingReturnGeneratedId,
    int OriginalTransactionId,
    string ReceivedCycleId,
    string ReasonCode,
    string TransactionCode,
    string TraceNumber,
    string OriginalTraceNumber,
    string OriginalReceivingDfi,
    string SourceReturnTraceNumber,
    string SourceReturnSettlementDate,
    string SourceReturnReasonCode,
    decimal Amount,
    DateTime ReceivedAtUtc,
    string IdempotencyKey);

public sealed record CenitReturnOfReturnResult(
    bool IsSuccessful,
    bool WasDuplicate,
    long? FlowId,
    int? ReturnOfReturnTransactionId,
    string Code,
    string Message);
