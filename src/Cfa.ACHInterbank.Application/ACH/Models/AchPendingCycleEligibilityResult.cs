namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>
/// Proyección no persistente. Los códigos son internos, NO causales ACH Rxx/Dxx.
/// </summary>
public sealed record AchPendingCycleEligibilityResult(
    int TransactionId,
    bool IsEligible,
    string ReasonCode,
    string? FromCycleId = null,
    string? ToCycleId = null,
    int? ClearingHouseId = null,
    DateTime? ProcessingDate = null);
