using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Services;

/// <summary>
/// Adaptador puro entre el request transaccional legacy y el contrato canónico de onboarding.
/// Si la transacción no trae identidad suficiente, no inventa datos y devuelve null.
/// </summary>
public static class ThirdPartyOnboardingRequestMapper
{
    public static ThirdPartyOnboardingRequest? TryCreate(
        AchTransactionRequestData transaction,
        Customer? sourceCustomer,
        Customer? recipientCustomer,
        ThirdPartyAccountOriginSource source)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (sourceCustomer is null
            || sourceCustomer.Id <= 0
            || recipientCustomer is null
            || string.IsNullOrWhiteSpace(transaction.RecipientIdNumber)
            || string.IsNullOrWhiteSpace(transaction.RecipientName)
            || string.IsNullOrWhiteSpace(transaction.DestinationAccountNumber)
            || transaction.DestinationInstitutionId <= 0
            || string.IsNullOrWhiteSpace(recipientCustomer.DocumentType)
            || string.IsNullOrWhiteSpace(recipientCustomer.PersonType))
        {
            return null;
        }

        return new ThirdPartyOnboardingRequest(
            sourceCustomer.Id,
            recipientCustomer.DocumentType,
            transaction.RecipientIdNumber,
            transaction.RecipientName,
            recipientCustomer.PersonType,
            transaction.DestinationInstitutionId,
            transaction.AccountType,
            transaction.DestinationAccountNumber,
            source);
    }
}
