using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Base de ciclo de vida para cancelación/archivado (preparación evolutiva).
/// </summary>
[Scoped]
public sealed class BulkIngestionLifecycleService : IBulkIngestionLifecycleService
{
    private readonly AchDbContext _context;

    public BulkIngestionLifecycleService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<bool> RequestCancellationAsync(Guid batchId, string requestedBy, CancellationToken ct = default)
    {
        var batch = await _context.BulkIngestionBatches.FirstOrDefaultAsync(x => x.Id == batchId, ct);
        if (batch is null)
        {
            return false;
        }

        if (batch.Status is BulkIngestionBatchStatusEnum.Completed or BulkIngestionBatchStatusEnum.Failed or BulkIngestionBatchStatusEnum.Cancelled)
        {
            return false;
        }

        batch.Status = BulkIngestionBatchStatusEnum.Cancelled;
        batch.LastJobMessage = $"Cancelado por {requestedBy} a las {DateTime.UtcNow:O}.";
        batch.ProcessingFinishedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> ArchiveExpiredBatchesAsync(DateTime utcNow, CancellationToken ct = default)
    {
        // Placeholder para futura estrategia de archivado físico (cold storage / warehouse).
        // Por ahora solo marca lotes muy antiguos como cancelados si siguen abiertos.
        var threshold = utcNow.AddDays(-90);
        var affected = await _context.BulkIngestionBatches
            .Where(x => x.UploadedAtUtc < threshold
                        && x.Status != BulkIngestionBatchStatusEnum.Completed
                        && x.Status != BulkIngestionBatchStatusEnum.Failed
                        && x.Status != BulkIngestionBatchStatusEnum.Cancelled)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, BulkIngestionBatchStatusEnum.Cancelled)
                .SetProperty(x => x.LastJobMessage, "Cierre automático por expiración operativa."), ct);

        return affected;
    }
}
