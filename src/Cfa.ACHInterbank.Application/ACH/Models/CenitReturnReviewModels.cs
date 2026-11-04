namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>Solicitud de segunda persona. La identidad del decisor viene del token, nunca del cuerpo.</summary>
public sealed record CenitReturnReviewDecision(
    string Action, string ExpectedStatus, string Justification, string? EvidenceReference);

/// <summary>La aprobación NO implica ejecución financiera ni elegibilidad automática futura.</summary>
public sealed record CenitReturnReviewItem(
    Guid Id, int OriginalTransactionId, Guid SourceExecutionId, string ReasonCode,
    string OriginalCycleId, string SuggestedReturnCycleId, string Status,
    DateTime PreparedAtUtc, string? ReviewedByUserId, DateTime? ReviewedAtUtc,
    string? DecisionJustification, string? DecisionEvidenceReference,
    bool ProcessingConnected);
