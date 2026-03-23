using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record TransactionPolicyPreviewRequest(
    decimal Amount,
    string Reference,
    TransactionTypeEnum Type,
    AccountTypeEnum AccountType,
    bool IsPrenotification,
    int DestinationInstitutionId,
    string SourceAccountNumber,
    string DestinationAccountNumber,
    string CompanyIdentification,
    string? RecipientIdNumber);

public sealed record TransactionPolicyPreview(
    bool CanSubmit,
    string? Message,
    string? CycleId,
    string? CycleName,
    DateTime? ProcessingDate,
    string? ClearingHouseName,
    int? ClearingHouseId,
    string? WindowLabel,
    bool IsWithinProcessingWindow,
    decimal? MaxAmountPerTransaction,
    decimal? RemainingAmountForCycle,
    int? RemainingTransactionsForCycle,
    string? IdempotencyKey,
    bool WouldDuplicate);
