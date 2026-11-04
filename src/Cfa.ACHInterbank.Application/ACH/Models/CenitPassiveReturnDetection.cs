namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>I02-CENIT: hallazgos consultados a demanda; no equivalen a devoluciones autorizadas.</summary>
public sealed record CenitPassiveReturnFinding(
    int OriginalTransactionId,
    Guid QueueId,
    Guid ExecutionId,
    string Classification,
    string? VerifiedReasonCode,
    string Explanation,
    DateTime EvidenceAtUtc);

public sealed record CenitPassiveReturnDetection(
    string Mode,
    bool ProcessingConnected,
    int ExaminedTransactions,
    bool LimitedWindow,
    IReadOnlyList<CenitPassiveReturnFinding> Findings);
