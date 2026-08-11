using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public enum CenitPrenotificationDirection
{
    Unknown = 0,
    Debit = 1,
    Credit = 2
}

public enum CenitIncomingReturnPolicyStatus
{
    Allowed = 1,
    Rejected = 2,
    ManualReviewRequired = 3
}

public sealed record CenitReturnCauseDefinition(
    string Code,
    string Description,
    bool AppliesToDebitPrenotification,
    bool AppliesToDebitMonetary,
    bool AppliesToCreditPrenotification,
    bool AppliesToCreditMonetary,
    int? MaxCalendarDays);

public sealed record CenitIncomingReturnPolicyRequest(
    TransactionTypeEnum OriginalTransactionType,
    string ReturnReasonCode,
    DateTime OriginalValueDate,
    DateTime ReturnValueDate,
    int? OriginalCycleNumber,
    int? ReturnCycleNumber,
    int? LastReturnCycleNumber,
    decimal OriginalAmount,
    decimal ReturnedAmount,
    CenitPrenotificationDirection PrenotificationDirection = CenitPrenotificationDirection.Unknown,
    DateTime? ReturnRequestDate = null,
    bool? ImmediateReturnCycleConfirmed = null,
    bool? FundsAvailabilityRequired = null,
    bool? FundsAvailabilityConfirmed = null,
    bool? ConfirmationToOriginatorRecorded = null,
    DateTime? ReceiverRejectionDeadlineDate = null);

public sealed record CenitIncomingReturnOperationalEvidence(
    CenitPrenotificationDirection PrenotificationDirection = CenitPrenotificationDirection.Unknown,
    DateTime? ReturnRequestDate = null,
    bool? ImmediateReturnCycleConfirmed = null,
    bool? FundsAvailabilityRequired = null,
    bool? FundsAvailabilityConfirmed = null,
    bool? ConfirmationToOriginatorRecorded = null,
    DateTime? ReceiverRejectionDeadlineDate = null);

public sealed record CenitIncomingReturnPolicyResult(
    CenitIncomingReturnPolicyStatus Status,
    string Code,
    string Message)
{
    public bool IsAllowed => Status == CenitIncomingReturnPolicyStatus.Allowed;
    public bool RequiresManualReview => Status == CenitIncomingReturnPolicyStatus.ManualReviewRequired;
}
