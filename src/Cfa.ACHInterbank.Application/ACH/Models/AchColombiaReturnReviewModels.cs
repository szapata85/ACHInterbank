namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>Consulta acotada de candidatas ACH Colombia. Ningún estado implica archivo generado.</summary>
public sealed record AchColombiaReturnReviewItem(
    long Id, int OriginalTransactionId, string Reference, decimal Amount,
    string TransactionType, string OriginalCycleId, string? SuggestedReturnCycleId,
    string ReasonCode, string Status, DateTime PreparedAtUtc, bool CanApprove,
    string? BlockingReason, bool CanGenerateFile = false);

public sealed record AchColombiaReturnReviewDecision(string Action);

public sealed record AchColombiaReturnReviewResult(long Id, string Status);

public sealed class AchColombiaReturnReviewConflictException(string message) : Exception(message) { }
