namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>Resultado operacional interno. Los códigos NO son causales bancarias.</summary>
public sealed record AchPendingCycleReassignmentResult(
    int TransactionId,
    bool Moved,
    string ReasonCode,
    string? FromCycleId = null,
    string? ToCycleId = null,
    int? FromBatchId = null,
    int? ToBatchId = null);
