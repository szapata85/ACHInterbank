using System.Data;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class CenitCycleQueueService : ICenitCycleQueueService
{
    private readonly ICenitOperatingCalendarPolicy _calendarPolicy;
    private readonly AchDbContext _context;

    public CenitCycleQueueService(ICenitOperatingCalendarPolicy calendarPolicy, AchDbContext context)
    {
        _calendarPolicy = calendarPolicy;
        _context = context;
    }

    public async Task<CenitCycleQueue> EnqueueAsync(AchTransaction transaction, DateTime receivedAtUtc, string reason, CancellationToken ct)
    {
        var clearingHouseId = await _context.AchCycles
            .Where(x => x.Id == transaction.AchCycleId)
            .Select(x => x.ClearingHouseId)
            .FirstAsync(ct);
        var targetCycle = await _calendarPolicy.ResolveTargetCycleAsync(clearingHouseId, receivedAtUtc, ct);
        var existing = await _context.CenitCycleQueues
            .Where(x => x.AchTransactionId == transaction.Id)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
        if (existing is not null)
        {
            if (existing.Status == "Consumed") return existing; // Reintento tras incorporación.
            if (existing.Status != "Queued" || existing.TargetAchCycleId != targetCycle.Id)
                throw new InvalidOperationException("CENIT_QUEUE_DUPLICATE_TARGET_CONFLICT");
            return existing;
        }

        var queue = new CenitCycleQueue
        {
            AchTransactionId = transaction.Id,
            OriginalAchCycleId = transaction.AchCycleId,
            TargetAchCycleId = targetCycle.Id,
            QueueReason = reason,
            Status = "Queued",
            EnqueuedAtUtc = DateTime.UtcNow
        };

        _context.CenitCycleQueues.Add(queue);
        await _context.SaveChangesAsync(ct);
        return queue;
    }

    // F03: se invoca antes de leer los lotes del archivo NACHA-M CENIT.
    // La cola solo se consume cuando transacción, lote, evento y totales quedan
    // confirmados dentro de una misma transacción relacional.
    public async Task<int> IncorporateForCycleAsync(string targetAchCycleId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(targetAchCycleId))
            throw new ArgumentException("Se requiere ciclo destino CENIT.", nameof(targetAchCycleId));

        if (_context.ChangeTracker.HasChanges())
            throw new InvalidOperationException("CENIT_QUEUE_CONTEXT_DIRTY: no incorporar con mutaciones pendientes.");

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            try
            {
                if (!_context.Database.IsRelational())
                    return await IncorporateCoreAsync(targetAchCycleId, ct);

                await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                var moved = await IncorporateCoreAsync(targetAchCycleId, ct);
                await dbTransaction.CommitAsync(ct);
                return moved;
            }
            catch
            {
                _context.ChangeTracker.Clear();
                throw;
            }
        });
    }

    private async Task<int> IncorporateCoreAsync(string targetAchCycleId, CancellationToken ct)
    {
        var target = await _context.AchCycles.AsNoTracking()
            .Include(x => x.ClearingHouse)
            .SingleOrDefaultAsync(x => x.Id == targetAchCycleId, ct)
            ?? throw new InvalidOperationException("CENIT_QUEUE_TARGET_NOT_FOUND");
        if (!string.Equals(target.ClearingHouse?.Code, "CENIT", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("CENIT_QUEUE_WRONG_CHAMBER");
        if (target.OperationalStatus == AchCycleOperationalStatus.Cancelled)
            throw new InvalidOperationException("CENIT_QUEUE_TARGET_CANCELLED");

        var queued = await _context.CenitCycleQueues
            .Include(x => x.AchTransaction).ThenInclude(x => x.AchBatch)
            .Where(x => x.TargetAchCycleId == targetAchCycleId && x.Status == "Queued")
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
        if (queued.Count == 0) return 0;

        // No añadir transacciones a un archivo ya exportado: podría retransmitirse
        // una versión distinta bajo el mismo identificador de archivo.
        if (await _context.AchFileExports.AsNoTracking().AnyAsync(x =>
            x.AchCycleId == targetAchCycleId, ct))
            throw new InvalidOperationException("CENIT_QUEUE_TARGET_ALREADY_EXPORTED");

        var touchedBatchIds = new HashSet<int>();
        var occurred = DateTime.UtcNow;
        foreach (var item in queued)
        {
            var tx = item.AchTransaction;
            var sourceBatch = tx?.AchBatch;
            if (tx is null || sourceBatch is null
                || tx.State != AchTransferStateEnum.Pending
                || tx.AchCycleId != item.OriginalAchCycleId
                || sourceBatch.AchCycleId != item.OriginalAchCycleId)
                throw new InvalidOperationException($"CENIT_QUEUE_SOURCE_CHANGED:{item.Id}");

            var source = await _context.AchCycles.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == item.OriginalAchCycleId, ct);
            if (source is null || source.ClearingHouseId != target.ClearingHouseId)
                throw new InvalidOperationException($"CENIT_QUEUE_CROSS_CHAMBER:{item.Id}");

            // La fecha valor se revisa en F04: jamás cambiarla silenciosamente.
            if (tx.EffectiveEntryDate.Date != target.ProcessingDate.Date
                || sourceBatch.EffectiveEntryDate.Date != tx.EffectiveEntryDate.Date)
                throw new InvalidOperationException($"CENIT_QUEUE_VALUE_DATE_REVIEW_REQUIRED:{item.Id}");

            if (await _context.AchFileExportTransactions.AsNoTracking()
                .AnyAsync(x => x.AchTransactionId == tx.Id, ct)
                || await _context.ContrapartidaDispatchItems.AsNoTracking()
                    .AnyAsync(x => x.AchTransactionId == tx.Id, ct))
                throw new InvalidOperationException($"CENIT_QUEUE_ALREADY_PRESENTED:{item.Id}");

            // La misma llave de lote que BatchResolver evita la pérdida del detalle
            // NACHA-M al cambiar el ciclo de la transacción.
            var targetBatch = item.OriginalAchCycleId == targetAchCycleId
                ? sourceBatch
                : await _context.AchBatches.FirstOrDefaultAsync(x =>
                x.AchCycleId == targetAchCycleId
                && x.CompanyName == sourceBatch.CompanyName
                && x.CompanyIdentification == sourceBatch.CompanyIdentification
                && x.CompanyEntryDescription == sourceBatch.CompanyEntryDescription
                && x.EffectiveEntryDate == sourceBatch.EffectiveEntryDate, ct);
            if (targetBatch is not null && (
                targetBatch.OriginOrOdfi != sourceBatch.OriginOrOdfi
                || targetBatch.CompanyEntryDescriptionId != sourceBatch.CompanyEntryDescriptionId))
                throw new InvalidOperationException($"CENIT_QUEUE_BATCH_INCOMPATIBLE:{item.Id}");
            if (targetBatch is null)
            {
                targetBatch = new AchBatch
                {
                    AchCycleId = targetAchCycleId,
                    CompanyName = sourceBatch.CompanyName,
                    CompanyIdentification = sourceBatch.CompanyIdentification,
                    CompanyEntryDescription = sourceBatch.CompanyEntryDescription,
                    CompanyEntryDescriptionId = sourceBatch.CompanyEntryDescriptionId,
                    OriginOrOdfi = sourceBatch.OriginOrOdfi,
                    EffectiveEntryDate = sourceBatch.EffectiveEntryDate,
                    ServiceClassCode = sourceBatch.ServiceClassCode,
                    BatchSequenceNumber = 0
                };
                _context.AchBatches.Add(targetBatch);
                await _context.SaveChangesAsync(ct); // Dentro del mismo commit serializable.
            }

            var oldBatchId = sourceBatch.Id;
            tx.AchBatchId = targetBatch.Id;
            tx.AchBatch = targetBatch;
            tx.AchCycleId = targetAchCycleId;
            // Sin tocar State, StateChangedAtUtc, fecha valor o compensación.
            item.Status = "Consumed";
            item.DequeuedAtUtc = occurred;
            var execution = await _context.CenitCycleExecutions
                .FirstOrDefaultAsync(x => x.AchCycleId == targetAchCycleId, ct);
            if (execution is not null) item.CenitCycleExecutionId = execution.Id;

            _context.AchTransactionStateEvents.Add(new AchTransactionStateEvent
            {
                AchTransactionId = tx.Id,
                FromState = AchTransferStateEnum.Pending,
                ToState = AchTransferStateEnum.Pending,
                Source = AchStateEventSourceEnum.System,
                ReasonCode = "CENIT_QUEUE_ATTACHED",
                IdempotencyKey = $"CENIT-Q:{item.Id}:{tx.Id}:{targetAchCycleId}",
                ClearingHouseId = target.ClearingHouseId,
                OccurredAtUtc = occurred,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    queueId = item.Id, fromCycleId = source.Id, targetCycleId = targetAchCycleId,
                    fromBatchId = oldBatchId, toBatchId = targetBatch.Id
                })
            });
            touchedBatchIds.Add(oldBatchId);
            touchedBatchIds.Add(targetBatch.Id);
        }

        await _context.SaveChangesAsync(ct);
        foreach (var batchId in touchedBatchIds)
        {
            var batch = await _context.AchBatches.SingleAsync(x => x.Id == batchId, ct);
            var details = await _context.AchTransactions.AsNoTracking()
                .Where(x => x.AchBatchId == batchId)
                .Select(x => new { x.Type, x.Amount })
                .ToListAsync(ct);
            batch.TotalDebitAmount = details.Where(x => x.Type is
                TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal)
                .Sum(x => x.Amount);
            batch.TotalCreditAmount = details.Where(x => x.Type is
                TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification)
                .Sum(x => x.Amount);
            if (details.Count > 0)
                batch.ServiceClassCode = AchBatchServiceClassPolicy.Resolve(details.Select(x => x.Type));
        }
        await _context.SaveChangesAsync(ct);
        return queued.Count;
    }
}
