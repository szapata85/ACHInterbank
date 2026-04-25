namespace Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

public sealed record PaymentRailOperationalContext(
    int? ClearingHouseId,
    string? ClearingHouseCode,
    string? AchCycleId,
    DateTime? OperationalDate,
    string CorrelationId);

public sealed record PaymentRailResolveRequest(
    int? ClearingHouseId,
    string? ClearingHouseCode,
    string? RequestedRailCode);

public sealed record PaymentRailResolveResult(
    string RailCode,
    bool IsKnownRail,
    string ResolutionSource,
    string Message);

public sealed record PaymentRailBridgeRequest(
    PaymentRailOperationalContext Context,
    string OperationName,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record PaymentRailBridgeResult(
    bool IsAllowed,
    string RailCode,
    string ResultCode,
    string Message);
