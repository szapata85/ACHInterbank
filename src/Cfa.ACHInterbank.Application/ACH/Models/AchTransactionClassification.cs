using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record AchTransactionClassificationRequest(
    TransactionTypeEnum TransactionType,
    bool IsPrenotification,
    bool SourceInstitutionIsDefault,
    bool DestinationInstitutionIsDefault);

public sealed record AchTransactionClassificationResult(
    AchTransactionDirection Direction,
    AchTransactionOrigin Origin,
    AchMonetaryIntegrationRoute MonetaryIntegrationRoute,
    AchTransactionClassificationStatus Status,
    bool SourceInstitutionWasDefaultAtCreation,
    DateTime ClassifiedAtUtc,
    int ClassificationVersion,
    string? RejectionMessage)
{
    public bool CanCreate => Status == AchTransactionClassificationStatus.Determined;
}
