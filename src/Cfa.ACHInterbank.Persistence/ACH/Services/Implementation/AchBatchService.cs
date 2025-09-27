using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchBatchService : IAchBatchService
{
    private readonly AchDbContext _context;

    public AchBatchService(AchDbContext context)
        => _context = context;

    /// <summary>
    /// Crea un lote de ACH asignándolo al próximo ciclo disponible de la cámara indicada.
    /// </summary>
    public async Task<AchBatch> CreateBatchAsync(
        int clearingHouseId,
        string companyName,
        string companyId,
        DateTime effectiveEntryDate,
        IEnumerable<int> transactionIds,
        CancellationToken ct = default)
    {
        // 1️⃣  Traer las transacciones a incluir en el lote
        var transactions = await _context.AchTransactions
            .Where(t => transactionIds.Contains(t.Id))
            .ToListAsync(ct);

        if (!transactions.Any())
            throw new InvalidOperationException("No se encontraron transacciones para el lote.");

        // 2️⃣  Buscar el ciclo más próximo de la cámara y para la fecha solicitada
        var cycle = await _context.AchCycles
            .Where(c =>
                c.ClearingHouseId == clearingHouseId &&
                c.ProcessingDate.Date == effectiveEntryDate.Date)
            .OrderBy(c => c.CutoffTime)
            .FirstOrDefaultAsync(ct);

        if (cycle == null)
            throw new InvalidOperationException(
                $"No existe ciclo para la cámara {clearingHouseId} en la fecha {effectiveEntryDate:yyyy-MM-dd}.");

        // 3️⃣  Crear el lote relacionándolo al ciclo encontrado
        var batch = new AchBatch
        {
            AchCycleId = cycle.Id,
            CompanyName = companyName,
            CompanyIdentification = companyId,
            EffectiveEntryDate = effectiveEntryDate,
            Transactions = transactions
        };

        _context.AchBatches.Add(batch);
        await _context.SaveChangesAsync(ct);

        return batch;
    }
}
