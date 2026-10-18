using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
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
    private const string CustomerThirdPartySavepoint = "TPA_CustomerThirdParty";

    private readonly IAchCustomerRepository _customerRepository;
    private readonly ICustomerThirdPartyRepository _customerThirdPartyRepository;
    private readonly AchDbContext? _dbContext;

    public PrenotificationHandler(
        IAchCustomerRepository customerRepository,
        ICustomerThirdPartyRepository customerThirdPartyRepository,
        AchDbContext? dbContext = null)
    {
        _customerRepository = customerRepository;
        _customerThirdPartyRepository = customerThirdPartyRepository;
        _dbContext = dbContext;
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

        var destinationAccountNumber = request.DestinationAccountNumber.Trim();
        var recipientIdNumber = request.RecipientIdNumber?.Trim().ToUpperInvariant() ?? string.Empty;
        CustomerThirdParty? existingThirdParty = null;
        if (customer.Id > 0)
        {
            existingThirdParty = await _customerThirdPartyRepository.FindAsync(
                customer.Id,
                request.DestinationInstitutionId,
                destinationAccountNumber,
                recipientIdNumber,
                ct);
        }

        if (existingThirdParty is not null)
        {
            LinkPendingPrenotificationUnlessFinal(existingThirdParty, transaction);
            return;
        }

        var thirdParty = new CustomerThirdParty
        {
            Customer = customer,
            CustomerId = customer.Id,
            DestinationInstitutionId = request.DestinationInstitutionId,
            DestinationAccountNumber = destinationAccountNumber,
            RecipientIdNumber = recipientIdNumber,
            PrenotificationTransaction = transaction
        };
        await _customerThirdPartyRepository.AddAsync(thirdParty, ct);

        // SINGLE/BULK already execute prenotification compatibility work inside an explicit database transaction.
        // Flush only inside that transaction so a natural-key race is detected before the orchestration leaves this
        // compatibility boundary. The savepoint keeps the flush retry-safe without committing the surrounding unit.
        var currentTransaction = _dbContext?.Database.CurrentTransaction;
        if (currentTransaction is null)
        {
            return;
        }

        await currentTransaction.CreateSavepointAsync(CustomerThirdPartySavepoint, ct);

        try
        {
            await _dbContext!.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            await currentTransaction.RollbackToSavepointAsync(CustomerThirdPartySavepoint, CancellationToken.None);

            var winner = await _customerThirdPartyRepository.FindAsync(
                customer.Id,
                request.DestinationInstitutionId,
                destinationAccountNumber,
                recipientIdNumber,
                ct);

            if (winner is null)
            {
                throw;
            }

            _dbContext.Entry(thirdParty).State = EntityState.Detached;
            LinkPendingPrenotificationUnlessFinal(winner, transaction);
        }
    }

    private static void LinkPendingPrenotificationUnlessFinal(
        CustomerThirdParty thirdParty,
        AchTransaction transaction)
    {
        if (thirdParty.Status is CustomerThirdPartyStatusEnum.Active or CustomerThirdPartyStatusEnum.Rejected)
        {
            return;
        }

        thirdParty.LinkPendingPrenotification(transaction);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception.InnerException; current is not null; current = current.InnerException)
        {
            var type = current.GetType();

            var sqlState = type.GetProperty("SqlState")?.GetValue(current)?.ToString();
            if (string.Equals(sqlState, "23505", StringComparison.Ordinal))
            {
                return true;
            }

            var numberValue = type.GetProperty("Number")?.GetValue(current);
            if (numberValue is int number && number is 2601 or 2627)
            {
                return true;
            }

            var errorCodeValue = type.GetProperty("SqliteErrorCode")?.GetValue(current);
            if (errorCodeValue is int errorCode && errorCode == 19)
            {
                return true;
            }
        }

        return false;
    }
}
