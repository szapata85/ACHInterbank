using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record CycleTransactionPolicyRequest(
    int ClearingHouseId,
    int? ClearingHouseCycleConfigId,
    DateTime OperationalDate,
    string? ClearingHouseCode,
    string? PaymentRailCode,
    string? CycleName,
    TransactionTypeEnum TransactionType,
    bool IsPrenotification,
    string? ReturnReasonCode = null,
    string? OriginalTraceRef = null,
    bool IsReturnOfReturn = false,
    string? TransactionCode = null);

public sealed record CycleTransactionPolicyResult(
    bool IsAllowed,
    string ReasonCode,
    string Message,
    string RailCode,
    int? CycleNumber,
    string FunctionalClass);

public sealed record ResolvedClearingHouseCyclePolicy(
    int ClearingHouseId,
    string ClearingHouseCode,
    string PolicyVersion,
    DateTime OperationalDate,
    string TimeZoneId,
    IReadOnlyList<Cfa.ACHInterbank.Domain.Models.ACH.ClearingHouseCycleConfig> Cycles);

