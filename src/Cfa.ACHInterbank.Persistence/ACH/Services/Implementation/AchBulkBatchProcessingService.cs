using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchBulkBatchProcessingService : IAchBulkBatchProcessingService
{
    private readonly AchDbContext _context;
    private readonly IAchBulkTransactionService _bulkTransactionService;
    private readonly ILogger<AchBulkBatchProcessingService> _logger;
    private readonly IBulkIngestionProgressNotifier _progressNotifier;

    public AchBulkBatchProcessingService(
        AchDbContext context,
        IAchBulkTransactionService bulkTransactionService,
        ILogger<AchBulkBatchProcessingService> logger,
        IBulkIngestionProgressNotifier progressNotifier)
    {
        _context = context;
        _bulkTransactionService = bulkTransactionService;
        _logger = logger;
        _progressNotifier = progressNotifier;
    }

    public async Task ProcessBatchAsync(Guid batchId, long? attemptId = null, string? jobId = null, CancellationToken ct = default)
    {
        var batch = await _context.BulkIngestionBatches
            .FirstOrDefaultAsync(x => x.Id == batchId, ct)
            ?? throw new InvalidOperationException($"No se encontró el lote {batchId}.");
        var attempt = attemptId.HasValue
            ? await _context.BulkIngestionAttempts.FirstOrDefaultAsync(x => x.Id == attemptId.Value && x.BatchId == batchId, ct)
            : null;

        if (batch.Status is not (BulkIngestionBatchStatusEnum.Queued or BulkIngestionBatchStatusEnum.Retrying or BulkIngestionBatchStatusEnum.Validated))
        {
            _logger.LogInformation("El lote {BatchId} no está en estado procesable. Estado actual: {Status}", batch.Id, batch.Status);
            return;
        }

        var isRetryingAttempt = batch.Status == BulkIngestionBatchStatusEnum.Retrying;
        batch.Status = BulkIngestionBatchStatusEnum.Processing;
        batch.ProcessingStartedAtUtc = DateTime.UtcNow;
        batch.LastJobId = jobId;
        batch.LastJobMessage = "Iniciando procesamiento asíncrono.";
        if (isRetryingAttempt)
        {
            batch.RetryCount++;
        }
        if (attempt is not null)
        {
            attempt.Status = BulkIngestionAttemptStatusEnum.Processing;
            attempt.StartedAtUtc = DateTime.UtcNow;
            attempt.JobId = jobId;
        }
        await _context.SaveChangesAsync(ct);
        await _progressNotifier.NotifyBatchProgressAsync(batch.Id, 0m, "Procesamiento iniciado.", ct);

        var readyItems = await _context.BulkIngestionItems
            .Where(i => i.BatchId == batchId && i.Status == BulkIngestionItemStatusEnum.Ready)
            .OrderBy(i => i.ItemIndex)
            .Select(i => new
            {
                Entity = i,
                i.ItemIndex,
                i.NormalizedPayloadJson
            })
            .ToListAsync(ct);

        if (readyItems.Count == 0)
        {
            batch.TotalProcessed = 0;
            batch.TotalSucceeded = 0;
            batch.TotalFailed = batch.TotalInvalid;
            batch.Status = BulkIngestionBatchStatusEnum.Failed;
            batch.ProcessingFinishedAtUtc = DateTime.UtcNow;
            batch.LastJobMessage = "No existen ítems listos para procesamiento.";
            await _progressNotifier.NotifyBatchProgressAsync(batch.Id, 100m, batch.LastJobMessage, ct);

            if (attempt is not null)
            {
                attempt.Status = BulkIngestionAttemptStatusEnum.Failed;
                attempt.FinishedAtUtc = DateTime.UtcNow;
                attempt.TotalProcessed = 0;
                attempt.TotalSucceeded = 0;
                attempt.TotalFailed = batch.TotalFailed;
                attempt.ResultMessage = batch.LastJobMessage;
            }
            await _context.SaveChangesAsync(ct);
            return;
        }

        var itemRequests = new List<BulkAchTransactionItemRequest>(readyItems.Count);
        var mapByResponseIndex = new List<BulkIngestionItem>(readyItems.Count);

        foreach (var item in readyItems)
        {
            if (string.IsNullOrWhiteSpace(item.NormalizedPayloadJson))
            {
                item.Entity.Status = BulkIngestionItemStatusEnum.StructuralError;
                item.Entity.Message = "Ítem sin payload normalizado para ejecución.";
                continue;
            }

            var request = JsonSerializer.Deserialize<BulkAchTransactionItemRequest>(item.NormalizedPayloadJson);
            if (request is null)
            {
                item.Entity.Status = BulkIngestionItemStatusEnum.StructuralError;
                item.Entity.Message = "No fue posible deserializar payload normalizado.";
                continue;
            }

            itemRequests.Add(request);
            mapByResponseIndex.Add(item.Entity);
        }

        if (itemRequests.Count == 0)
        {
            batch.TotalProcessed = 0;
            batch.TotalSucceeded = 0;
            batch.TotalFailed = batch.TotalInvalid;
            batch.Status = BulkIngestionBatchStatusEnum.Failed;
            batch.ProcessingFinishedAtUtc = DateTime.UtcNow;
            batch.LastJobMessage = "No existen ítems ejecutables tras normalización.";
            await _progressNotifier.NotifyBatchProgressAsync(batch.Id, 100m, batch.LastJobMessage, ct);
            if (attempt is not null)
            {
                attempt.Status = BulkIngestionAttemptStatusEnum.Failed;
                attempt.FinishedAtUtc = DateTime.UtcNow;
                attempt.TotalProcessed = 0;
                attempt.TotalSucceeded = 0;
                attempt.TotalFailed = batch.TotalFailed;
                attempt.ResultMessage = batch.LastJobMessage;
            }
            await _context.SaveChangesAsync(ct);
            return;
        }

        try
        {
            var result = await _bulkTransactionService.RegisterBulkAsync(new BulkAchTransactionRequest
            {
                BatchReference = batch.BatchReference,
                Transactions = itemRequests
            }, ct);

            foreach (var itemResult in result.ItemResults)
            {
                if (itemResult.Index < 0 || itemResult.Index >= mapByResponseIndex.Count)
                {
                    continue;
                }

                var entity = mapByResponseIndex[itemResult.Index];
                if (itemResult.Succeeded)
                {
                    entity.Status = BulkIngestionItemStatusEnum.Processed;
                    entity.TransactionId = itemResult.TransactionId;
                    entity.Message = "Procesado correctamente.";
                }
                else
                {
                    entity.Status = BulkIngestionItemStatusEnum.ProcessingError;
                    entity.Message = itemResult.ErrorMessage ?? itemResult.ErrorCode ?? "Error de procesamiento.";
                }
            }

            var processingErrors = mapByResponseIndex
                .Where(x => x.Status == BulkIngestionItemStatusEnum.ProcessingError && !string.IsNullOrWhiteSpace(x.Message))
                .Select(x => $"Ítem {x.ItemIndex}: {x.Message}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToList();

            batch.TotalProcessed = result.TotalProcessed;
            batch.TotalSucceeded = result.TotalSucceeded;
            batch.TotalFailed = batch.TotalInvalid + result.TotalFailed;

            batch.Status = result.TotalSucceeded == 0
                ? BulkIngestionBatchStatusEnum.Failed
                : result.TotalFailed > 0
                    ? BulkIngestionBatchStatusEnum.PartiallyProcessed
                    : BulkIngestionBatchStatusEnum.Completed;

            batch.ProcessingFinishedAtUtc = DateTime.UtcNow;
            batch.LastJobMessage = $"Procesamiento finalizado. Exitosas={result.TotalSucceeded}, Fallidas={result.TotalFailed}.";
            if (processingErrors.Count > 0)
            {
                batch.SummaryErrorsJson = JsonSerializer.Serialize(processingErrors);
            }
            await _progressNotifier.NotifyBatchProgressAsync(batch.Id, 100m, batch.LastJobMessage, ct);
            if (attempt is not null)
            {
                attempt.TotalProcessed = result.TotalProcessed;
                attempt.TotalSucceeded = result.TotalSucceeded;
                attempt.TotalFailed = result.TotalFailed;
                attempt.FinishedAtUtc = DateTime.UtcNow;
                attempt.Status = result.TotalSucceeded == 0
                    ? BulkIngestionAttemptStatusEnum.Failed
                    : result.TotalFailed > 0
                        ? BulkIngestionAttemptStatusEnum.PartiallyProcessed
                        : BulkIngestionAttemptStatusEnum.Completed;
                attempt.ResultMessage = batch.LastJobMessage;
            }

            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló procesamiento asíncrono del lote {BatchId}", batch.Id);

            foreach (var entity in mapByResponseIndex.Where(x => x.Status == BulkIngestionItemStatusEnum.Ready))
            {
                entity.Status = BulkIngestionItemStatusEnum.ProcessingError;
                entity.Message = "Error no controlado en job de procesamiento.";
            }

            batch.TotalProcessed = 0;
            batch.TotalSucceeded = 0;
            batch.TotalFailed = batch.TotalInvalid + mapByResponseIndex.Count;
            batch.Status = BulkIngestionBatchStatusEnum.Failed;
            batch.ProcessingFinishedAtUtc = DateTime.UtcNow;
            batch.LastJobMessage = ex.Message.Length > 1900 ? ex.Message[..1900] : ex.Message;
            batch.SummaryErrorsJson = JsonSerializer.Serialize(new[] { "Error no controlado durante el procesamiento asíncrono del lote." });
            await _progressNotifier.NotifyBatchProgressAsync(batch.Id, 100m, "Procesamiento finalizó con error.", ct);
            if (attempt is not null)
            {
                attempt.Status = BulkIngestionAttemptStatusEnum.Failed;
                attempt.FinishedAtUtc = DateTime.UtcNow;
                attempt.TotalProcessed = 0;
                attempt.TotalSucceeded = 0;
                attempt.TotalFailed = mapByResponseIndex.Count;
                attempt.ResultMessage = batch.LastJobMessage;
            }
            await _context.SaveChangesAsync(ct);
        }
    }
}
