using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class PrenotificationHandler : IPrenotificationHandler
{
    private readonly AchDbContext _context;

    public PrenotificationHandler(AchDbContext context)
    {
        _context = context;
    }

    public async Task HandleAsync(AchTransactionRequestData request, AchTransaction transaction, CancellationToken ct = default)
    {
        if (!request.IsPrenotification)
        {
            return;
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.AccountNumber == request.SourceAccountNumber, ct);

        if (customer is null)
        {
            // La prenotificación puede registrarse aunque la cuenta origen no esté mapeada a un Customer interno.
            // En este caso no se crea/actualiza CustomerThirdParty, pero la transacción ACH queda registrada.
            return;
        }

        var existingThirdParty = await _context.CustomerThirdParties
            .FirstOrDefaultAsync(t =>
                t.CustomerId == customer.Id &&
                t.DestinationInstitutionId == request.DestinationInstitutionId &&
                t.DestinationAccountNumber == request.DestinationAccountNumber &&
                t.RecipientIdNumber == (request.RecipientIdNumber ?? string.Empty), ct);

        if (existingThirdParty is not null)
        {
            existingThirdParty.Status = CustomerThirdPartyStatusEnum.Pending;
            existingThirdParty.PrenotificationTransactionId = transaction.Id;
            existingThirdParty.ValidationCycleId = null;
            existingThirdParty.ValidationReceivedAt = null;
            existingThirdParty.ValidationMessage = null;
        }
        else
        {
            var thirdParty = new CustomerThirdParty
            {
                CustomerId = customer.Id,
                DestinationInstitutionId = request.DestinationInstitutionId,
                DestinationAccountNumber = request.DestinationAccountNumber,
                RecipientIdNumber = request.RecipientIdNumber?.Trim() ?? string.Empty,
                Status = CustomerThirdPartyStatusEnum.Pending,
                PrenotificationTransactionId = transaction.Id
            };
            _context.CustomerThirdParties.Add(thirdParty);
        }

        await _context.SaveChangesAsync(ct);
    }
}
