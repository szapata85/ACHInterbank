namespace Cfa.ACHInterbank.Application.ACH.Models;

public enum IncomingNachaOrphanResolutionAction
{
    MarkAsIgnored = 1,
    MarkAsRejected = 2,
    KeepPending = 3,
    LinkToTransaction = 4
}

public sealed class IncomingNachaOrphanManualResolutionRequest
{
    public Guid? IncomingNachaTransactionLinkId { get; init; }
    public Guid? IncomingNachaFileIngestionId { get; init; }
    public int? EntryDetailId { get; init; }
    public int? AddendaRecordId { get; init; }
    public IncomingNachaOrphanResolutionAction ResolutionAction { get; init; } = IncomingNachaOrphanResolutionAction.MarkAsIgnored;
    public int? ResolvedAchTransactionId { get; init; }
    public string ResolutionReason { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
    public string ResolvedBy { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
}

public sealed record IncomingNachaOrphanManualResolutionResult(
    bool IsResolved,
    string Status,
    Guid? ProcessingEventId,
    Guid? AchTransactionStateEventId,
    string Message);
