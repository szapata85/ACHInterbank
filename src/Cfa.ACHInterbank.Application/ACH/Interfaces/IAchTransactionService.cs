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
    IEnumerable<(string addendaType, string information)>? addendas = null,
    CancellationToken ct = default);


    // 🔹 Devuelve la próxima fecha hábil (saltando fines de semana y festivos)
    Task<DateTime> GetNextBusinessDayAsync(DateTime baseDate, CancellationToken ct = default);

    // 🔹 Obtiene transacciones por ciclo; opcionalmente incluye relaciones
    Task<IReadOnlyList<AchTransaction>> GetTransactionsByCycleAsync(
        int achCycleId,
        bool includeRelations = false,
        CancellationToken ct = default);
}


