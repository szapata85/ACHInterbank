using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchTransactionService
{
    
    Task<AchTransaction> RegisterTransactionAsync(
    decimal amount,
    string reference,
    TransactionTypeEnum type,
    int destinationInstitutionId,
    IEnumerable<(string addendaType, string information)>? addendas = null,
    CancellationToken ct = default);

    //Task<List<AchTransaction>> GetTransactionsByCycleAsync(
    //    int achCycleId,
    //    CancellationToken ct = default);

    //DateTime GetNextBusinessDay(DateTime date);
}

