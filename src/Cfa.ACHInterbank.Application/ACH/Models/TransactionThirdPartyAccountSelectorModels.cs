using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record TransactionThirdPartyAccountSelectorRequest(
    string SourceAccountNumber,
    int DestinationInstitutionId,
    TransactionTypeEnum TransactionType,
    int CompanyEntryDescriptionId);

public sealed record TransactionThirdPartyAccountSelectorResponse(
    int CustomerId,
    int DestinationInstitutionId,
    int ClearingHouseId,
    string ClearingHouseCode,
    string ClearingHouseName,
    TransactionNature TransactionNature,
    DateTime EffectiveDate,
    string ServiceType,
    IReadOnlyList<TransactionThirdPartyAccountSelectorItem> Items);

public sealed record TransactionThirdPartyAccountSelectorItem(
    long ThirdPartyAccountId,
    long ThirdPartyId,
    int DestinationInstitutionId,
    string DestinationInstitutionName,
    string DestinationAccountNumber,
    AccountTypeEnum AccountType,
    string RecipientIdNumber,
    string RecipientName,
    string RecipientPersonType,
    ThirdPartyAccountEligibilityDecision EligibilityDecision,
    string EligibilityCode,
    string EligibilityMessage,
    bool IsSelectable);
