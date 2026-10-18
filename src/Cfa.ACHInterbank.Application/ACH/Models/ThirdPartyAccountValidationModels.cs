using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record ThirdPartyAccountEligibilityRequest(
    long ThirdPartyAccountId,
    int ClearingHouseId,
    TransactionNature TransactionNature,
    DateTime EffectiveDate,
    string? ServiceType = null);

public sealed record ThirdPartyAccountEligibilityResult(
    ThirdPartyAccountEligibilityDecision Decision,
    string Code,
    string Message,
    ClearingHouseTransactionRule? PolicyRule,
    ThirdPartyAccountValidationStatus? ValidationStatus,
    long? ValidationId,
    long? AttemptId);

public sealed record PrenotificationValidationStartRequest(
    long ThirdPartyAccountId,
    int AchTransactionId,
    int ClearingHouseId,
    TransactionNature TransactionNature,
    DateTime EffectiveDate,
    DateTimeOffset SentAtUtc,
    string SourceCycleId,
    string IdempotencyKey,
    string? ServiceType = null,
    string? SourceFileReference = null);

public sealed record PrenotificationValidationStartResult(
    long ValidationId,
    long AttemptId,
    int AttemptNumber,
    int? AchTransactionId,
    ThirdPartyAccountValidationStatus ValidationStatus,
    PrenotificationValidationAttemptStatus AttemptStatus,
    DateTimeOffset? ResponseDeadlineAtUtc,
    bool ReusedExistingAttempt);

public sealed record PrenotificationValidationResolutionRequest(
    int AchTransactionId,
    AchTransferStateEnum TransactionState,
    DateTimeOffset ResolvedAtUtc,
    string EvidenceReference,
    string? CorrelationReference = null,
    string? ReturnCode = null,
    string? ReturnDetail = null,
    string? SourceCycleId = null);

public sealed record ClearingHouseValidationPolicyResolution(
    ClearingHouseTransactionRule? Rule,
    DateTime EffectiveDate,
    TransactionNature TransactionNature,
    int ClearingHouseId);


/// <summary>Evidence that a contextual validation authorized a monetary use of its canonical account.</summary>
public sealed record ThirdPartyAccountMonetaryUseRequest(
    long ThirdPartyAccountId,
    long ValidationId,
    DateTimeOffset UsedAtUtc);
