using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record ClearingHouseTransactionRuleDto(
    int Id,
    int ClearingHouseId,
    string ClearingHouseName,
    TransactionNature TransactionNature,
    TransactionTypeEnum TransactionType,
    bool RequiresPrenotification,
    PrenotificationRequirementMode PrenotificationMode,
    bool RequiresReceiverIdentificationValidation,
    ValidationRequirementMode ReceiverIdentificationValidationMode,
    bool AppliesToNachaExport,
    bool AppliesToMonetaryTransactions,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive,
    string NormativeSource,
    string NormativeReference,
    string Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateClearingHouseTransactionRuleRequest(
    int ClearingHouseId,
    TransactionNature TransactionNature,
    TransactionTypeEnum TransactionType,
    bool RequiresPrenotification,
    PrenotificationRequirementMode PrenotificationMode,
    bool RequiresReceiverIdentificationValidation,
    ValidationRequirementMode ReceiverIdentificationValidationMode,
    bool AppliesToNachaExport,
    bool AppliesToMonetaryTransactions,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string NormativeSource,
    string NormativeReference,
    string? Notes);

public sealed record UpdateClearingHouseTransactionRuleRequest(
    int ClearingHouseId,
    TransactionNature TransactionNature,
    TransactionTypeEnum TransactionType,
    bool RequiresPrenotification,
    PrenotificationRequirementMode PrenotificationMode,
    bool RequiresReceiverIdentificationValidation,
    ValidationRequirementMode ReceiverIdentificationValidationMode,
    bool AppliesToNachaExport,
    bool AppliesToMonetaryTransactions,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string NormativeSource,
    string NormativeReference,
    string? Notes);

public sealed record TransactionPrerequisitePreviewRequest(
    int ClearingHouseId,
    TransactionTypeEnum TransactionType,
    DateTime EffectiveEntryDate,
    bool AppliesToNachaExport = true);

public sealed record TransactionPrerequisitePreviewResponse(
    bool RuleConfigured,
    bool RequiresPrenotification,
    PrenotificationRequirementMode PrenotificationMode,
    bool RequiresReceiverIdentificationValidation,
    ValidationRequirementMode ReceiverIdentificationValidationMode,
    string? NormativeSource,
    string? NormativeReference,
    string Decision,
    string Message);
