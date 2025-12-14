using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
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
        string sourceAccountNumber,
        string destinationAccountNumber,
        IEnumerable<AddendaDto>? addendas = null,
        CancellationToken ct = default);

    Task<DateTime> GetNextBusinessDayAsync(DateTime baseDate, CancellationToken ct = default);

    Task<IReadOnlyList<AchTransaction>> GetTransactionsByCycleAsync(
        string achCycleId,
        bool includeRelations = false,
        CancellationToken ct = default);

    Task<AchTransaction?> GetTransactionByIdAsync(int transactionId, CancellationToken ct = default);

    Task<IReadOnlyList<AchTransactionListDto>> GetAllAsync(
        string? achCycleId = default,
        DateTime? effectiveDate = default,
        int? clearingHouseId = default,
        CancellationToken ct = default);
}
