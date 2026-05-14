namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record AchReturnEligibilityRequest(
    int TransactionId,
    string ReturnReasonCode,
    DateTime ReturnDate,
    bool HasAddenda,
    string? RequestedBy = null,
    string? Source = null);

public sealed record AchReturnEligibilityFailure(
    string Code,
    string Message,
    string? Field = null,
    string Severity = "Error");

public sealed record AchReturnEligibilityResult(
    bool IsEligible,
    string? NormalizedReasonCode,
    int? ClearingHouseId,
    string? TransactionType,
    string? CurrentState,
    IReadOnlyCollection<AchReturnEligibilityFailure> Failures);
