using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class PrenotificationHandler : IPrenotificationHandler
{
    private readonly IAchCustomerRepository _customerRepository;
    private readonly ICustomerThirdPartyRepository _customerThirdPartyRepository;

    public PrenotificationHandler(
        IAchCustomerRepository customerRepository,
        ICustomerThirdPartyRepository customerThirdPartyRepository)
    {
        _customerRepository = customerRepository;
        _customerThirdPartyRepository = customerThirdPartyRepository;
    }

    public async Task HandleAsync(AchTransactionRequestData request, AchTransaction transaction, CancellationToken ct = default)
    {
        if (!request.IsPrenotification)
        {
            return;
        }

        var customer = await _customerRepository.FindBySourceAccountNumberAsync(request.SourceAccountNumber, ct);

        if (customer is null)
        {
            // La prenotificación puede registrarse aunque la cuenta origen no esté mapeada a un Customer interno.
            // En este caso no se crea/actualiza CustomerThirdParty, pero la transacción ACH queda registrada.
            return;
        }

        var recipientIdNumber = request.RecipientIdNumber?.Trim() ?? string.Empty;
        CustomerThirdParty? existingThirdParty = null;
        if (customer.Id > 0)
        {
            existingThirdParty = await _customerThirdPartyRepository.FindAsync(
                customer.Id,
                request.DestinationInstitutionId,
                request.DestinationAccountNumber,
                recipientIdNumber,
                ct);
        }

        if (existingThirdParty is not null)
        {
            existingThirdParty.Status = CustomerThirdPartyStatusEnum.Pending;
            existingThirdParty.PrenotificationTransaction = transaction;
            existingThirdParty.ValidationCycleId = null;
            existingThirdParty.ValidationReceivedAt = null;
            existingThirdParty.ValidationMessage = null;
        }
        else
        {
            var thirdParty = new CustomerThirdParty
            {
                Customer = customer,
                CustomerId = customer.Id,
                DestinationInstitutionId = request.DestinationInstitutionId,
                DestinationAccountNumber = request.DestinationAccountNumber,
                RecipientIdNumber = recipientIdNumber,
                Status = CustomerThirdPartyStatusEnum.Pending,
                PrenotificationTransaction = transaction
            };
            await _customerThirdPartyRepository.AddAsync(thirdParty, ct);
        }
    }
}
