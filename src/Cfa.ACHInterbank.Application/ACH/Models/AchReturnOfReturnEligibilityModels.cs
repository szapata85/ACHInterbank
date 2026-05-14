namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record AchReturnOfReturnEligibilityRequest(
    int SourceReturnTransactionId,
    string NewReturnReasonCode,
    DateTime RequestedAtUtc,
    string? RequestedBy = null,
    string? Source = null);

public sealed record AchReturnOfReturnEligibilityFailure(
    string Code,
    string Message,
    string? Field = null);

public sealed record AchReturnOfReturnEligibilityResult(
    bool IsEligible,
    int? ClearingHouseId,
    int? SourceReturnTransactionId,
    string? OriginalReturnReasonCode,
    string? NewReturnReasonCode,
    IReadOnlyCollection<AchReturnOfReturnEligibilityFailure> Failures);
