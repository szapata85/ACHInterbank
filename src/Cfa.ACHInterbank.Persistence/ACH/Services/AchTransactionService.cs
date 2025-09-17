using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services;

[Scoped]
public class AchTransactionService : IAchTransactionService
{
    private readonly AchDbContext _context;
    private readonly IRoutingStrategyService _routing;
    private readonly IBankHoliday _holidayService;

    public AchTransactionService(
        AchDbContext context,
        IRoutingStrategyService routing,
        IBankHoliday holidayService)
    {
        _context = context;
        _routing = routing;
        _holidayService = holidayService;
    }

    public async Task<AchTransaction> RegisterTransactionAsync(
        decimal amount,
        string reference,
        TransactionTypeEnum type,
        int destinationInstitutionId,
        IEnumerable<(string addendaType, string information)>? addendas = null,
        CancellationToken ct = default)
    {
        if (amount <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(reference)) throw new ArgumentException("Referencia obligatoria.");

        // ✅ 1. Obtener institución origen por defecto
        var sourceId = await _context.FinancialInstitutions
            .Where(fi => fi.IsDefaultSource)
            .Select(fi => fi.Id)
            .FirstOrDefaultAsync(ct);

        if (sourceId == 0)
            throw new InvalidOperationException("No existe institución financiera de origen por defecto.");

        // ✅ 2. Determinar ciclo de compensación considerando días hábiles
        var now = DateTime.Now;
        var cycleId = await _routing.ResolveClearingHouseForTransactionAsync(
            destinationInstitutionId, now, ct);

        // ✅ 3. Crear la transacción principal
        var tx = new AchTransaction
        {
            Amount = amount,
            Reference = reference,
            Type = type,
            SourceInstitutionId = sourceId,
            DestinationInstitutionId = destinationInstitutionId,
            AchCycleId = cycleId
        };

        _context.AchTransactions.Add(tx);
        await _context.SaveChangesAsync(ct);

        // ✅ 4. Registrar addendas si vienen
        if (addendas != null)
        {
            foreach (var (addendaType, information) in addendas)
            {
                var addenda = new AchTransactionAddenda
                {
                    AchTransactionId = tx.Id,
                    AddendaType = addendaType,
                    Information = information
                };
                _context.Set<AchTransactionAddenda>().Add(addenda);
            }
            await _context.SaveChangesAsync(ct);
        }

        return tx;
    }
}
