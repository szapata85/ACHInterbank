using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchBulkBatchQueryService : IAchBulkBatchQueryService
{
    private readonly AchDbContext _context;

    public AchBulkBatchQueryService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<BulkBatchStatusDto?> GetBatchAsync(Guid batchId, CancellationToken ct = default)
    {
        var batch = await _context.BulkIngestionBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == batchId, ct);

        return batch is null ? null : MapStatus(batch);
    }

    public async Task<BulkBatchItemsPageDto> GetBatchItemsAsync(Guid batchId, int page, int pageSize, BulkIngestionItemStatusEnum? status, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var query = _context.BulkIngestionItems
            .AsNoTracking()
            .Where(x => x.BatchId == batchId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.ItemIndex)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BulkBatchItemDto
            {
                ItemId = x.Id,
                ItemIndex = x.ItemIndex,
                Reference = x.Reference,
                Status = x.Status,
                Message = x.Message,
                TransactionId = x.TransactionId
            })
            .ToListAsync(ct);

        return new BulkBatchItemsPageDto
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }

    public async Task<BulkBatchProcessingSummaryDto?> GetBatchSummaryAsync(Guid batchId, CancellationToken ct = default)
    {
        var batch = await _context.BulkIngestionBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == batchId, ct);

        if (batch is null)
        {
            return null;
        }

        var attempts = await _context.BulkIngestionAttempts
            .AsNoTracking()
            .Where(x => x.BatchId == batchId)
            .OrderByDescending(x => x.AttemptNumber)
            .Select(x => new BulkBatchAttemptDto
            {
                AttemptId = x.Id,
                AttemptNumber = x.AttemptNumber,
                TriggerType = x.TriggerType.ToString(),
                Scope = x.Scope.ToString(),
                TriggeredBy = x.TriggeredBy,
                TriggeredAtUtc = x.TriggeredAtUtc,
                Status = x.Status.ToString(),
                JobId = x.JobId,
                StartedAtUtc = x.StartedAtUtc,
                FinishedAtUtc = x.FinishedAtUtc,
                TotalProcessed = x.TotalProcessed,
                TotalSucceeded = x.TotalSucceeded,
                TotalFailed = x.TotalFailed,
                ResultMessage = x.ResultMessage
            })
            .ToListAsync(ct);

        return new BulkBatchProcessingSummaryDto
        {
            BatchId = batchId,
            Status = MapStatus(batch),
            Attempts = attempts
        };
    }

    private static BulkBatchStatusDto MapStatus(BulkIngestionBatch batch)
    {
        var denominator = Math.Max(batch.TotalValid, 1);
        var progress = batch.Status switch
        {
            BulkIngestionBatchStatusEnum.Queued => 0m,
            BulkIngestionBatchStatusEnum.Processing => Math.Round((decimal)batch.TotalProcessed / denominator * 100m, 2),
            BulkIngestionBatchStatusEnum.Completed => 100m,
            BulkIngestionBatchStatusEnum.PartiallyProcessed => 100m,
            BulkIngestionBatchStatusEnum.Failed when batch.TotalProcessed > 0 => 100m,
            _ => 0m
        };

        return new BulkBatchStatusDto
        {
            BatchId = batch.Id,
            BatchReference = batch.BatchReference,
            Status = batch.Status,
            TotalRecords = batch.TotalRecords,
            TotalValid = batch.TotalValid,
            TotalInvalid = batch.TotalInvalid,
            TotalProcessed = batch.TotalProcessed,
            TotalSucceeded = batch.TotalSucceeded,
            TotalFailed = batch.TotalFailed,
            ProgressPercent = progress,
            UploadedAtUtc = batch.UploadedAtUtc,
            ProcessingStartedAtUtc = batch.ProcessingStartedAtUtc,
            ProcessingFinishedAtUtc = batch.ProcessingFinishedAtUtc,
            RetryCount = batch.RetryCount,
            LastJobId = batch.LastJobId,
            LastJobMessage = batch.LastJobMessage,
            ErrorSummary = DeserializeSummary(batch.SummaryErrorsJson)
        };
    }

    private static IReadOnlyList<string> DeserializeSummary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
