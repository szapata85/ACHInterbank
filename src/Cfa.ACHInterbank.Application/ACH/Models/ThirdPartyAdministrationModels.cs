using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record ThirdPartyAdministrationQuery(
    string? Search = null,
    ThirdPartyAdministrativeStatus? Status = null,
    int Page = 1,
    int PageSize = 20);

public sealed record ThirdPartyAdministrationDto
{
    public long ThirdPartyId { get; init; }
    public int CustomerId { get; init; }
    public string PersonType { get; init; } = string.Empty;
    public string DocumentType { get; init; } = string.Empty;
    public string DocumentNumberMasked { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public ThirdPartyAdministrativeStatus Status { get; init; }
    public int AccountCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record CreateThirdPartyCommand(
    string DocumentType,
    string DocumentNumber,
    string DisplayName,
    string PersonType);

public sealed record UpdateThirdPartyCommand(
    string DisplayName);

public sealed record CreateThirdPartyAccountCommand(
    int FinancialInstitutionId,
    AccountTypeEnum AccountType,
    string AccountNumber);

public sealed record UpdateThirdPartyAccountCommand(
    ThirdPartyAdministrativeStatus Status);

public sealed record SetThirdPartyAdministrativeStatusCommand(
    ThirdPartyAdministrativeStatus Status);

public sealed record ThirdPartyCommandResult<T>(
    T Value,
    bool Created,
    bool ReusedExisting,
    bool RecoveredFromUniqueConflict);

public sealed record ThirdPartyAccountValidationHistoryDto
{
    public long ValidationId { get; init; }
    public long ThirdPartyAccountId { get; init; }
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
    public IReadOnlyList<PrenotificationValidationAttemptHistoryDto> Attempts { get; init; } = [];
}

public sealed record PrenotificationValidationAttemptHistoryDto
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

public sealed record RequestThirdPartyAccountRevalidationCommand(
    int ClearingHouseId,
    TransactionNature TransactionNature,
    DateTime EffectiveDate,
    string ServiceType,
    int CompanyEntryDescriptionId,
    string SourceAccountNumber,
    string IdempotencyKey);

public sealed record ThirdPartyAccountRevalidationResult(
    int AchTransactionId,
    long ValidationId,
    long AttemptId,
    int AttemptNumber,
    ThirdPartyAccountValidationStatus ValidationStatus,
    PrenotificationValidationAttemptStatus AttemptStatus,
    DateTimeOffset? ResponseDeadlineAtUtc,
    bool ReusedExistingAttempt);

public sealed record CanonicalPrenotificationTransactionRequest(
    long ThirdPartyAccountId,
    int ExpectedClearingHouseId,
    DateTime ExpectedEffectiveDate,
    string ValidationIdempotencyKey,
    TransactionNature TransactionNature,
    AccountTypeEnum AccountType,
    int DestinationInstitutionId,
    string SourceAccountNumber,
    string DestinationAccountNumber,
    string CompanyName,
    string CompanyIdentification,
    int CompanyEntryDescriptionId,
    string SourcePersonType,
    string RecipientPersonType,
    string RecipientIdNumber,
    string RecipientName);

public sealed record AchTransactionRegistrationOptions(
    long? CanonicalThirdPartyAccountId = null,
    int? ExpectedClearingHouseId = null,
    DateTime? ExpectedEffectiveDate = null,
    string? PrenotificationValidationIdempotencyKey = null);

public sealed class ThirdPartyAdministrationConflictException : InvalidOperationException
{
    public ThirdPartyAdministrationConflictException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}

public sealed class PrenotificationIdempotencyConflictException : InvalidOperationException
{
    public PrenotificationIdempotencyConflictException(string idempotencyKey)
        : base("La solicitud de revalidación ya fue materializada por una ejecución concurrente.")
    {
        IdempotencyKey = idempotencyKey;
    }

    public string IdempotencyKey { get; }
}
