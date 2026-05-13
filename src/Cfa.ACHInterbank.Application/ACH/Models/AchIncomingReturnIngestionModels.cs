namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record AchIncomingReturnIngestionRequest(
    string FileName,
    string RawContent,
    DateTime ReceivedAtUtc,
    string? Source = null,
    string? UploadedBy = null);

public sealed record AchIncomingReturnIngestionFailure(
    string Code,
    string Message,
    string? Field = null,
    string? TraceNumber = null,
    string Severity = "Error");

public sealed record AchIncomingReturnItem(
    string? TraceNumber,
    string? OriginalTraceNumber,
    string? ReturnReasonCode,
    int? OriginalTransactionId,
    int? ClearingHouseId,
    string? TransactionType,
    string? CurrentState,
    bool IsLinked,
    string? RawRecord);

public sealed record AchIncomingReturnIngestionResult(
    bool IsAccepted,
    int TotalRecords,
    int ParsedReturnCount,
    int LinkedReturnCount,
    int UnlinkedReturnCount,
    IReadOnlyCollection<AchIncomingReturnItem> Items,
    IReadOnlyCollection<AchIncomingReturnIngestionFailure> Failures);
