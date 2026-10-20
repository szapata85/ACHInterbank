using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record ThirdPartyAccountReadQuery
{
    public string? Search { get; init; }
    public int? CustomerId { get; init; }
    public long? ThirdPartyId { get; init; }
    public long? ThirdPartyAccountId { get; init; }
    public int? FinancialInstitutionId { get; init; }
    public ThirdPartyAdministrativeStatus? ThirdPartyStatus { get; init; }
    public ThirdPartyAdministrativeStatus? AccountStatus { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public ThirdPartyAccountEligibilityReadContext? EligibilityContext { get; init; }
}

public sealed record ThirdPartyAccountEligibilityReadContext(
    int ClearingHouseId,
    TransactionNature TransactionNature,
    DateTime EffectiveDate,
    string? ServiceType = null);

public sealed record ThirdPartyAccountReadDto
{
    public long ThirdPartyAccountId { get; init; }
    public long ThirdPartyId { get; init; }
    public int CustomerId { get; init; }
    public string CustomerDisplayName { get; init; } = string.Empty;
    public string ThirdPartyDisplayName { get; init; } = string.Empty;
    public string PersonType { get; init; } = string.Empty;
    public string DocumentType { get; init; } = string.Empty;
    public string DocumentNumberMasked { get; init; } = string.Empty;
    public ThirdPartyAdministrativeStatus ThirdPartyStatus { get; init; }
    public int FinancialInstitutionId { get; init; }
    public string FinancialInstitutionName { get; init; } = string.Empty;
    public AccountTypeEnum AccountType { get; init; }
    public string AccountNumberMasked { get; init; } = string.Empty;
    public ThirdPartyAdministrativeStatus AccountStatus { get; init; }
    public ThirdPartyAccountOriginSource? OriginSource { get; init; }
    public IReadOnlyList<ThirdPartyAccountValidationReadDto> Validations { get; init; } = [];
    public ThirdPartyAccountEligibilityReadDto? Eligibility { get; init; }
}

public sealed record ThirdPartyAccountValidationReadDto
{
    public long ValidationId { get; init; }
    public int ClearingHouseId { get; init; }
    public string ClearingHouseCode { get; init; } = string.Empty;
    public string ClearingHouseName { get; init; } = string.Empty;
    public TransactionNature TransactionNature { get; init; }
    public string ServiceType { get; init; } = string.Empty;
    public ThirdPartyAccountValidationType ValidationType { get; init; }
    public ThirdPartyAccountValidationStatus Status { get; init; }
    public int? PolicyRuleId { get; init; }
    public DateTime? PolicyEffectiveFrom { get; init; }
    public DateTime EffectiveDate { get; init; }
    public DateTimeOffset? ExpiresAtUtc { get; init; }
    public DateTimeOffset? LastMonetaryUseAtUtc { get; init; }
    public PrenotificationValidationAttemptReadDto? LatestAttempt { get; init; }
}

public sealed record PrenotificationValidationAttemptReadDto
{
    public long AttemptId { get; init; }
    public int AttemptNumber { get; init; }
    public int? AchTransactionId { get; init; }
    public PrenotificationValidationAttemptStatus Status { get; init; }
    public PrenotificationValidationResolution Resolution { get; init; }
    public DateTimeOffset SentAtUtc { get; init; }
    public DateTimeOffset? ResponseDeadlineAtUtc { get; init; }
    public DateTimeOffset? ResolvedAtUtc { get; init; }
    public string? ReturnCode { get; init; }
    public string? SourceCycleId { get; init; }
}

public sealed record ThirdPartyAccountEligibilityReadDto
{
    public ThirdPartyAccountEligibilityDecision Decision { get; init; }
    public string Code { get; init; } = string.Empty;
    public int? PolicyRuleId { get; init; }
    public long? ValidationId { get; init; }
    public ThirdPartyAccountValidationStatus? ValidationStatus { get; init; }
    public long? AttemptId { get; init; }
}

public enum ThirdPartyAccountShadowDiscrepancyType
{
    Match = 1,
    LegacyOnly = 2,
    CanonicalOnly = 3,
    StatusMismatch = 4,
    EligibilityMismatch = 5,
    IdentityIncomplete = 6,
    PolicyContextMissing = 7
}

public sealed record ThirdPartyAccountShadowSnapshot(
    bool LegacyExists,
    bool CanonicalExists,
    bool IdentityComplete,
    bool PolicyContextComplete,
    CustomerThirdPartyStatusEnum? LegacyStatus,
    ThirdPartyAccountValidationStatus? CanonicalValidationStatus,
    ThirdPartyAccountEligibilityDecision? CanonicalEligibilityDecision);

public sealed record ThirdPartyAccountShadowComparisonResult(
    ThirdPartyAccountShadowDiscrepancyType Discrepancy,
    int? LegacyThirdPartyId = null,
    long? CanonicalThirdPartyAccountId = null);

public sealed record ThirdPartyAccountShadowComparisonSummary(
    int Compared,
    IReadOnlyDictionary<ThirdPartyAccountShadowDiscrepancyType, int> Counts);
