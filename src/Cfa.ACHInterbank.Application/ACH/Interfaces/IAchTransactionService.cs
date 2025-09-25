using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchTransactionService
{
    /// <summary>
    /// Registra una nueva transacción ACH y crea automáticamente
    /// el lote (Batch) y el ciclo si es necesario.
    /// </summary>
    /// <param name="amount">Monto de la transacción en pesos colombianos.</param>
    /// <param name="reference">Referencia de la transacción.</param>
    /// <param name="type">Tipo de transacción (Crédito/Débito).</param>
    /// <param name="destinationInstitutionId">Id de la entidad financiera destino.</param>
    /// <param name="sourceAccountNumber">Número de cuenta de origen.</param>
    /// <param name="destinationAccountNumber">Número de cuenta de destino.</param>
    /// <param name="addendas">Colección opcional de addendas.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<AchTransaction> RegisterTransactionAsync(
        decimal amount,
        string reference,
        TransactionTypeEnum type,
        int destinationInstitutionId,
        string sourceAccountNumber,
        string destinationAccountNumber,
        IEnumerable<(string addendaType, string information)>? addendas = null,
        CancellationToken ct = default);

    /// <summary>
    /// Devuelve la próxima fecha hábil (omite fines de semana y festivos).
    /// </summary>
    Task<DateTime> GetNextBusinessDayAsync(DateTime baseDate, CancellationToken ct = default);

    /// <summary>
    /// Obtiene todas las transacciones de un ciclo ACH, con opción de incluir
    /// las relaciones (instituciones, addendas, etc.).
    /// </summary>
    Task<IReadOnlyList<AchTransaction>> GetTransactionsByCycleAsync(
        int achCycleId,
        bool includeRelations = false,
        CancellationToken ct = default);
}


