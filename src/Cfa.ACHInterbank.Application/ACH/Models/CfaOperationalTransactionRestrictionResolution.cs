using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record CfaOperationalTransactionRestrictionRequest(
    int ClearingHouseId,
    int? ClearingHouseCycleConfigId,
    TransactionTypeEnum TransactionType,
    bool IsPrenotification,
    AccountTypeEnum AccountType,
    DateTime EffectiveAt);

// Operational restrictions only tighten a regulatory decision; this result never grants regulatory permission.
public sealed record CfaOperationalTransactionRestrictionResolution(
    bool IsOperationallyDenied,
    string? DenyReason,
    decimal? MaxAmountPerTransaction,
    decimal? MaxAmountPerCycle,
    int? MaxTransactionsPerCycle,
    IReadOnlyList<AccountTypeEnum>? AllowedAccountTypes,
    IReadOnlyList<int> AppliedRestrictionIds);
