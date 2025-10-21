using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchTransactionService
{
    /// <summary>
    /// Registra una transacción ACH asegurando consistencia con el ciclo y lote activo.
    /// </summary>
    /// <param name="amount">Monto de la transacción.</param>
    /// <param name="reference">Referencia única o descripción del movimiento.</param>
    /// <param name="type">Tipo de transacción (Crédito/Débito).</param>
    /// <param name="destinationInstitutionId">ID de la institución financiera destino.</param>
    /// <param name="sourceAccountNumber">Número de cuenta origen.</param>
    /// <param name="destinationAccountNumber">Número de cuenta destino.</param>
    /// <param name="companyName">Nombre de la empresa o entidad que origina el pago.</param>
    /// <param name="companyIdentification">Identificación de la compañía.</param>
    /// <param name="companyEntryDescription">Descripción breve de la transacción (PPD, CCD, etc.).</param>
    /// <param name="addendas">Addendas opcionales con información adicional.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Instancia persistida de <see cref="AchTransaction"/>.</returns>
    Task<AchTransaction> RegisterTransactionAsync(
        decimal amount,
        string reference,
        TransactionTypeEnum type,
        int destinationInstitutionId,
        string sourceAccountNumber,
        string destinationAccountNumber,
        string companyName,
        string companyIdentification,
        string companyEntryDescription,
        IEnumerable<(string addendaType, string information)>? addendas = null,
        CancellationToken ct = default);

    /// <summary>
    /// Obtiene la próxima fecha hábil (omite fines de semana y festivos).
    /// </summary>
    Task<DateTime> GetNextBusinessDayAsync(DateTime baseDate, CancellationToken ct = default);

    /// <summary>
    /// Retorna todas las transacciones asociadas a un ciclo.
    /// </summary>
    Task<IReadOnlyList<AchTransaction>> GetTransactionsByCycleAsync(
        int achCycleId,
        bool includeRelations = false,
        CancellationToken ct = default);
}



