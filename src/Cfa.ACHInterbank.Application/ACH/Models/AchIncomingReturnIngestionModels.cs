namespace Cfa.ACHInterbank.Application.ACH.Models;

public static class AchIncomingReturnIngestionDecision
{
    public const string Accepted = "Accepted";
    public const string RejectedTotal = "RejectedTotal";
    public const string RejectedPartial = "RejectedPartial";
}

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

public sealed record AchIncomingReturnAuditRecord(
    int RecordIndex,
    string RecordType,
    string? TraceNumber,
    string? OriginalTraceNumber,
    string? ReturnReasonCode,
    int? OriginalTransactionId,
    int? ClearingHouseId,
    bool IsLinked,
    string RawRecordHash,
    string? RawRecordPreview);

public sealed record AchIncomingReturnAuditFailure(
    string Code,
    string Message,
    string? Field,
    string? TraceNumber,
    int? RecordIndex);

public sealed record AchIncomingReturnIngestionAudit(
    string FileName,
    DateTime ReceivedAtUtc,
    string? Source,
    string? UploadedBy,
    int RawContentLength,
    int TotalRecords,
    int ParsedReturnCount,
    int LinkedReturnCount,
    int UnlinkedReturnCount,
    int FailureCount,
    string Decision,
    string ContentSha256,
    IReadOnlyCollection<AchIncomingReturnAuditRecord> Records,
    IReadOnlyCollection<AchIncomingReturnAuditFailure> Failures);

public sealed record AchIncomingReturnIngestionResult(
    bool IsAccepted,
    string Decision,
    bool IsRejectedTotal,
    bool IsRejectedPartial,
    int TotalRecords,
    int ParsedReturnCount,
    int LinkedReturnCount,
    int UnlinkedReturnCount,
    IReadOnlyCollection<AchIncomingReturnItem> Items,
    IReadOnlyCollection<AchIncomingReturnIngestionFailure> Failures,
    AchIncomingReturnIngestionAudit Audit);
