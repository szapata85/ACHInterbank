using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchBulkBatchRetryService : IAchBulkBatchRetryService
{
    private const int MaxRetryAttempts = 3;

    private readonly AchDbContext _context;
    private readonly IAchBulkJobScheduler _jobScheduler;

    public AchBulkBatchRetryService(AchDbContext context, IAchBulkJobScheduler jobScheduler)
    {
        _context = context;
        _jobScheduler = jobScheduler;
    }

    public async Task<RetryBatchResponse> RetryAsync(Guid batchId, RetryBatchRequest request, string triggeredBy, CancellationToken ct = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        var batch = await _context.BulkIngestionBatches
            .FirstOrDefaultAsync(x => x.Id == batchId, ct)
            ?? throw new KeyNotFoundException($"No existe el lote {batchId}.");

        if (batch.Status is BulkIngestionBatchStatusEnum.Queued or BulkIngestionBatchStatusEnum.Processing or BulkIngestionBatchStatusEnum.Retrying)
        {
            throw new InvalidOperationException("El lote ya tiene un procesamiento en curso.");
        }

        if (batch.RetryCount >= MaxRetryAttempts)
        {
            throw new InvalidOperationException($"El lote alcanzó el máximo de {MaxRetryAttempts} reintentos permitidos.");
        }

        var processableQuery = _context.BulkIngestionItems.Where(x => x.BatchId == batchId);
        switch (request.Scope)
        {
            case BulkIngestionRetryScopeEnum.FailedOnly:
                processableQuery = processableQuery.Where(x => x.Status == BulkIngestionItemStatusEnum.ProcessingError);
                break;
            case BulkIngestionRetryScopeEnum.Full:
                processableQuery = processableQuery.Where(x => x.Status != BulkIngestionItemStatusEnum.StructuralError);
                break;
        }

        var processableItems = await processableQuery.ToListAsync(ct);
        if (processableItems.Count == 0)
        {
            throw new InvalidOperationException("No existen ítems elegibles para reintento con el alcance seleccionado.");
        }

        foreach (var item in processableItems)
        {
            item.Status = BulkIngestionItemStatusEnum.Ready;
            item.Message = "Pendiente por reintento.";
            item.TransactionId = null;
        }

        var nextAttemptNumber = await _context.BulkIngestionAttempts
            .Where(x => x.BatchId == batchId)
            .Select(x => (int?)x.AttemptNumber)
            .MaxAsync(ct) ?? 0;
        nextAttemptNumber++;

        var attempt = new BulkIngestionAttempt
        {
            BatchId = batchId,
            AttemptNumber = nextAttemptNumber,
            TriggerType = BulkIngestionTriggerTypeEnum.Retry,
            Scope = request.Scope,
            TriggeredBy = string.IsNullOrWhiteSpace(triggeredBy) ? "system" : triggeredBy,
            TriggeredAtUtc = DateTime.UtcNow,
            Status = BulkIngestionAttemptStatusEnum.Queued,
            ResultMessage = "Reintento encolado."
        };

        batch.Status = BulkIngestionBatchStatusEnum.Retrying;
        batch.LastJobMessage = "Reintento solicitado y encolado.";
        batch.QueuedAtUtc = DateTime.UtcNow;

        _context.BulkIngestionAttempts.Add(attempt);
        await _context.SaveChangesAsync(ct);

        var jobId = await _jobScheduler.EnqueueBatchAsync(batchId, attempt.Id, ct);
        attempt.JobId = jobId;
        batch.LastJobId = jobId;
        await _context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new RetryBatchResponse
        {
            BatchId = batchId,
            AttemptId = attempt.Id,
            AttemptNumber = attempt.AttemptNumber,
            JobId = jobId,
            Status = batch.Status
        };
    }
}
