using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record OrdinaryTransactionCodeAuthorityContext(
    string ClearingHouseCode,
    int ClearingHouseId,
    string CycleName,
    DateTime ProcessDate,
    string SourceServiceClassCode);

public sealed record OrdinaryTransactionCodeSemanticRequest(
    TransactionTypeEnum Type,
    AccountTypeEnum AccountType,
    bool IsPrenotification);
