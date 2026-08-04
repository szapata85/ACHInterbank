using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record CycleTransactionPolicyRequest(
    string? ClearingHouseCode,
    string? PaymentRailCode,
    string? CycleName,
    TransactionTypeEnum TransactionType,
    bool IsPrenotification,
    string? ReturnReasonCode = null,
    string? OriginalTraceRef = null);

public sealed record CycleTransactionPolicyResult(
    bool IsAllowed,
    string ReasonCode,
    string Message,
    string RailCode,
    int? CycleNumber,
    string FunctionalClass);

