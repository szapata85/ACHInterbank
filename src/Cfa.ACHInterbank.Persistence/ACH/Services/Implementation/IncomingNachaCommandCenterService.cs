using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaCommandCenterService : IIncomingNachaCommandCenterService
{
    private readonly AchDbContext _context;
    private readonly IIncomingNachaStateMachineService _stateMachineService;

    public IncomingNachaCommandCenterService(
        AchDbContext context,
        IIncomingNachaStateMachineService stateMachineService)
    {
        _context = context;
        _stateMachineService = stateMachineService;
    }

    public async Task<IncomingNachaObservabilitySummaryDto> GetObservabilitySummaryAsync(int windowHours = 24, CancellationToken ct = default)
    {
        var safeWindowHours = Math.Clamp(windowHours, 1, 168);
        var fromUtc = DateTime.UtcNow.AddHours(-safeWindowHours);
        var nowUtc = DateTime.UtcNow;
        var fromOffsetUtc = new DateTimeOffset(fromUtc, TimeSpan.Zero);

        var ingestionsQ = _context.IncomingNachaFileIngestions
            .AsNoTracking()
            .Where(x => x.UploadedAtUtc >= fromUtc);

        var queueQ = _context.IncomingNachaDispatchQueue
            .AsNoTracking()
            .Where(x => x.CreatedAt >= fromOffsetUtc);

        var eventsQ = _context.IncomingNachaProcessingEvents
            .AsNoTracking()
            .Where(x => x.OccurredAtUtc >= fromUtc);

        var totalIngestions = await ingestionsQ.CountAsync(ct);
        var totalQueueItems = await queueQ.CountAsync(ct);
        var backlogItems = await queueQ.CountAsync(x =>
            x.QueueStatus != IncomingNachaDispatchQueueStatus.Confirmed
            && x.QueueStatus != IncomingNachaDispatchQueueStatus.FailedFinal, ct);
        var blockedItems = await queueQ.CountAsync(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.Blocked, ct);
        var retryPendingItems = await queueQ.CountAsync(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.RetryPending, ct);
        var waitingWindowItems = await queueQ.CountAsync(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.WaitingWindow, ct);
        var failedFinalItems = await queueQ.CountAsync(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.FailedFinal, ct);
        var confirmedItems = await queueQ.CountAsync(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.Confirmed, ct);

        var createdAtValues = totalQueueItems > 0
            ? await queueQ.Select(x => x.CreatedAt).ToListAsync(ct)
            : [];
        var ageMinutes = createdAtValues.Select(x => Math.Max(0d, (nowUtc - x.UtcDateTime).TotalMinutes)).ToList();
        var averageQueueAgeMinutes = ageMinutes.Count > 0 ? ageMinutes.Average() : 0d;
        var oldestQueueAgeMinutes = ageMinutes.Count > 0 ? ageMinutes.Max() : 0d;

        var ingestionsByStatus = await ingestionsQ
            .GroupBy(x => x.IngestionStatus)
            .Select(g => new IncomingNachaKpiCountDto(g.Key.ToString(), g.Count()))
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        var queueByStatus = await queueQ
            .GroupBy(x => x.QueueStatus)
            .Select(g => new IncomingNachaKpiCountDto(g.Key.ToString(), g.Count()))
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        var byClearingCycle = await queueQ
            .GroupBy(x => new { x.ClearingHouseId, x.AchCycleId })
            .Select(g => new IncomingNachaClearingCycleKpiDto(
                g.Key.ClearingHouseId,
                g.Key.AchCycleId,
                g.Count(),
                g.Count(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.Blocked),
                g.Count(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.RetryPending),
                g.Count(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.WaitingWindow),
                g.Count(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.FailedFinal),
                g.Count(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.Confirmed)))
            .OrderByDescending(x => x.TotalItems)
            .Take(25)
            .ToListAsync(ct);

        var topErrors = await queueQ
            .Where(x => !string.IsNullOrWhiteSpace(x.LastErrorCode))
            .GroupBy(x => x.LastErrorCode)
            .Select(g => new IncomingNachaTopErrorDto(g.Key, g.Count(), g.Max(x => x.LastAttemptAtUtc)))
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(ct);

        var timelineEvents = await eventsQ
            .Select(x => new { x.OccurredAtUtc, x.EventStatus, x.Message })
            .ToListAsync(ct);
        var rawTimeline = timelineEvents
            .GroupBy(x => new DateTime(
                x.OccurredAtUtc.Year,
                x.OccurredAtUtc.Month,
                x.OccurredAtUtc.Day,
                x.OccurredAtUtc.Hour,
                0,
                0,
                DateTimeKind.Utc))
            .Select(g => new IncomingNachaTimelinePointDto(
                g.Key,
                g.Count(),
                g.Count(x => x.EventStatus == "Applied"),
                g.Count(x => x.EventStatus == "Rejected"),
                g.Count(x => x.Message.Contains("RetryPending", StringComparison.OrdinalIgnoreCase)),
                g.Count(x => x.Message.Contains("FailedFinal", StringComparison.OrdinalIgnoreCase))))
            .OrderBy(x => x.BucketAtUtc)
            .ToList();

        var timeline = BuildTimelineWithEmptyBuckets(fromUtc, nowUtc, rawTimeline);

        var pipelineHealth = new IncomingNachaPipelineHealthDto(
            totalIngestions,
            totalQueueItems,
            backlogItems,
            blockedItems,
            retryPendingItems,
            waitingWindowItems,
            failedFinalItems,
            confirmedItems,
            averageQueueAgeMinutes,
            oldestQueueAgeMinutes);

        return new IncomingNachaObservabilitySummaryDto(
            nowUtc,
            safeWindowHours,
            pipelineHealth,
            ingestionsByStatus,
            queueByStatus,
            byClearingCycle,
            topErrors,
            timeline);
    }

    public async Task<IncomingNachaPageResult<IncomingNachaIngestionListItemDto>> GetIngestionsAsync(IncomingNachaIngestionQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var q = _context.IncomingNachaFileIngestions.AsNoTracking().AsQueryable();
        if (query.IngestionStatus.HasValue)
        {
            q = q.Where(x => x.IngestionStatus == query.IngestionStatus.Value);
        }

        if (query.ParsingStatus.HasValue)
        {
            q = q.Where(x => x.ParsingStatus == query.ParsingStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.CorrelationId))
        {
            var val = query.CorrelationId.Trim();
            q = q.Where(x => x.CorrelationId.Contains(val));
        }

        if (!string.IsNullOrWhiteSpace(query.FileName))
        {
            var val = query.FileName.Trim();
            q = q.Where(x => x.FileName.Contains(val));
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.UploadedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new IncomingNachaIngestionListItemDto(
                x.Id,
                x.FileName,
                x.CorrelationId,
                x.IngestionStatus,
                x.CycleResolutionStatus,
                x.ParsingStatus,
                x.ResolvedClearingHouseId,
                x.ResolvedAchCycleId,
                x.OperationalDate,
                x.IsReprocess,
                x.UploadedAtUtc,
                _context.IncomingNachaDispatchQueue.Count(q => q.IncomingNachaFileIngestionId == x.Id),
                _context.IncomingNachaProcessingEvents.Count(e => e.IncomingNachaFileIngestionId == x.Id)))
            .ToListAsync(ct);

        return new IncomingNachaPageResult<IncomingNachaIngestionListItemDto>(items, page, pageSize, total);
    }

    public async Task<IncomingNachaIngestionDetailDto?> GetIngestionDetailAsync(Guid ingestionId, CancellationToken ct = default)
    {
        var ingestion = await _context.IncomingNachaFileIngestions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == ingestionId, ct);
        if (ingestion is null)
        {
            return null;
        }

        var queue = await GetQueueAsync(new IncomingNachaQueueQuery { IngestionId = ingestionId, Page = 1, PageSize = 200 }, ct);
        var events = await _context.IncomingNachaProcessingEvents
            .AsNoTracking()
            .Where(x => x.IncomingNachaFileIngestionId == ingestionId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(200)
            .Select(x => new IncomingNachaProcessingEventDto(x.Id, x.EventType, x.EventStatus, x.Message, x.OccurredAtUtc, x.RaisedBy, x.AchTransactionId))
            .ToListAsync(ct);

        return new IncomingNachaIngestionDetailDto(
            ingestion.Id,
            ingestion.FileName,
            ingestion.CorrelationId,
            ingestion.IngestionStatus,
            ingestion.CycleResolutionStatus,
            ingestion.ParsingStatus,
            ingestion.DetectedClearingHouseId,
            ingestion.ResolvedClearingHouseId,
            ingestion.ResolvedAchCycleId,
            ingestion.OperationalDate,
            ingestion.Notes,
            ingestion.IsReprocess,
            ingestion.ParentIngestionId,
            queue.Items,
            events);
    }

    public async Task<IncomingNachaPageResult<IncomingNachaQueueListItemDto>> GetQueueAsync(IncomingNachaQueueQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var q = _context.IncomingNachaDispatchQueue.AsNoTracking().AsQueryable();
        if (query.IngestionId.HasValue)
        {
            q = q.Where(x => x.IncomingNachaFileIngestionId == query.IngestionId.Value);
        }

        if (query.QueueStatus.HasValue)
        {
            q = q.Where(x => x.QueueStatus == query.QueueStatus.Value);
        }

        if (query.ClearingHouseId.HasValue)
        {
            q = q.Where(x => x.ClearingHouseId == query.ClearingHouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.AchCycleId))
        {
            var v = query.AchCycleId.Trim();
            q = q.Where(x => x.AchCycleId == v);
        }

        var total = await q.CountAsync(ct);
        var queueRows = await q.OrderBy(x => x.Priority)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new QueueProjection(
                x.Id,
                x.IncomingNachaFileIngestionId,
                x.AchTransactionId,
                x.AchCycleId,
                x.ClearingHouseId,
                x.QueueStatus,
                x.Priority,
                x.AttemptCount,
                x.NextAttemptAtUtc,
                x.LastAttemptAtUtc,
                x.LastErrorCode,
                x.LastErrorMessage,
                x.LastResponseCode,
                x.ConfirmedAtUtc,
                x.CreatedAt))
            .ToListAsync(ct);
        var items = queueRows.Select(ToQueueListDto).ToList();

        return new IncomingNachaPageResult<IncomingNachaQueueListItemDto>(items, page, pageSize, total);
    }

    public async Task<IncomingNachaQueueDetailDto?> GetQueueDetailAsync(Guid queueId, CancellationToken ct = default)
    {
        var queue = await _context.IncomingNachaDispatchQueue
            .AsNoTracking()
            .Include(x => x.Classification)
            .FirstOrDefaultAsync(x => x.Id == queueId, ct);

        if (queue is null)
        {
            return null;
        }

        var ingestion = await _context.IncomingNachaFileIngestions.AsNoTracking().FirstAsync(x => x.Id == queue.IncomingNachaFileIngestionId, ct);
        var executions = await _context.IncomingNachaIntegrationExecution.AsNoTracking()
            .Where(x => x.DispatchQueueId == queueId)
            .OrderByDescending(x => x.StartedAtUtc)
            .Select(x => new IncomingNachaIntegrationExecutionDto(
                x.Id, x.DispatchQueueId, x.MethodName, x.CorrelationId, x.ResponseCode, x.ResponseMessage,
                x.IsSuccess, x.IsRetryable, x.StartedAtUtc, x.FinishedAtUtc))
            .ToListAsync(ct);

        var events = await _context.IncomingNachaProcessingEvents.AsNoTracking()
            .Where(x => x.IncomingNachaFileIngestionId == queue.IncomingNachaFileIngestionId
                        && (!x.AchTransactionId.HasValue || x.AchTransactionId == queue.AchTransactionId))
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(100)
            .Select(x => new IncomingNachaProcessingEventDto(x.Id, x.EventType, x.EventStatus, x.Message, x.OccurredAtUtc, x.RaisedBy, x.AchTransactionId))
            .ToListAsync(ct);

        var queueDto = new IncomingNachaQueueListItemDto(
            queue.Id,
            queue.IncomingNachaFileIngestionId,
            queue.AchTransactionId,
            queue.AchCycleId,
            queue.ClearingHouseId,
            queue.QueueStatus,
            queue.Priority,
            queue.AttemptCount,
            queue.NextAttemptAtUtc,
            queue.LastAttemptAtUtc,
            queue.LastErrorCode,
            queue.LastErrorMessage,
            queue.LastResponseCode,
            queue.ConfirmedAtUtc,
            queue.CreatedAt,
            _stateMachineService.GetAllowedDispatchActions(queue.QueueStatus));

        var ingestionDto = new IncomingNachaIngestionListItemDto(
            ingestion.Id,
            ingestion.FileName,
            ingestion.CorrelationId,
            ingestion.IngestionStatus,
            ingestion.CycleResolutionStatus,
            ingestion.ParsingStatus,
            ingestion.ResolvedClearingHouseId,
            ingestion.ResolvedAchCycleId,
            ingestion.OperationalDate,
            ingestion.IsReprocess,
            ingestion.UploadedAtUtc,
            0,
            0);

        var classification = queue.Classification;
        var classificationDto = new IncomingNachaEntryClassificationDto(
            classification.Id,
            classification.EntryDetailId,
            classification.AddendaRecordId,
            classification.FunctionalClass,
            classification.EligibilityStatus,
            classification.RequiresManualResolution,
            classification.ReturnReasonCode,
            classification.PrenoteStatus,
            classification.BusinessMeaning);

        return new IncomingNachaQueueDetailDto(queueDto, ingestionDto, classificationDto, executions, events);
    }

    public Task<IncomingNachaManualActionResultDto> RetryManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default)
        => ApplyManualActionAsync(queueId, request, performedBy, IncomingNachaDispatchEvent.ManualRetry, (q, req) =>
        {
            q.NextAttemptAtUtc = DateTime.UtcNow;
            q.LastErrorCode = string.Empty;
            q.LastErrorMessage = string.Empty;
            if (req.Priority.HasValue)
            {
                q.Priority = Math.Clamp(req.Priority.Value, 1, 999);
            }

            return "Retry manual aplicado.";
        }, ct);

    public Task<IncomingNachaManualActionResultDto> UnblockManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default)
        => ApplyManualActionAsync(queueId, request, performedBy, IncomingNachaDispatchEvent.ManualUnblock, (q, req) =>
        {
            q.NextAttemptAtUtc = DateTime.UtcNow;
            q.LastErrorCode = string.Empty;
            q.LastErrorMessage = string.Empty;
            if (req.Priority.HasValue)
            {
                q.Priority = Math.Clamp(req.Priority.Value, 1, 999);
            }

            return "Unblock manual aplicado.";
        }, ct);

    public Task<IncomingNachaManualActionResultDto> RequeueManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default)
        => ApplyManualActionAsync(queueId, request, performedBy, IncomingNachaDispatchEvent.ManualRequeue, (q, req) =>
        {
            q.NextAttemptAtUtc = DateTime.UtcNow;
            if (req.Priority.HasValue)
            {
                q.Priority = Math.Clamp(req.Priority.Value, 1, 999);
            }

            return "Requeue manual aplicado.";
        }, ct);

    public Task<IncomingNachaManualActionResultDto> MarkFailedFinalManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default)
        => ApplyManualActionAsync(queueId, request, performedBy, IncomingNachaDispatchEvent.ManualMarkFailedFinal, (q, _) =>
        {
            q.NextAttemptAtUtc = null;
            return "Marcado manual como FailedFinal.";
        }, ct);

    private async Task<IncomingNachaManualActionResultDto> ApplyManualActionAsync(
        Guid queueId,
        IncomingNachaManualActionRequest request,
        string performedBy,
        IncomingNachaDispatchEvent transitionEvent,
        Func<IncomingNachaDispatchQueue, IncomingNachaManualActionRequest, string> apply,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Justification) || request.Justification.Trim().Length < 8)
        {
            throw new InvalidOperationException("La justificación es obligatoria y debe tener al menos 8 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new InvalidOperationException("IdempotencyKey es obligatorio para acciones manuales.");
        }

        var normalizedKey = request.IdempotencyKey.Trim();
        var normalizedBy = string.IsNullOrWhiteSpace(performedBy) ? "ops.command-center" : performedBy;

        var tx = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(ct)
            : null;
        await using var _ = tx;

        var queue = await _context.IncomingNachaDispatchQueue.FirstOrDefaultAsync(x => x.Id == queueId, ct)
                    ?? throw new InvalidOperationException("No existe item de cola indicado.");

        var idempotentEventType = "DispatchTransition";
        var idempotentMessage = $"Event:{transitionEvent};IdempotencyKey:{normalizedKey}";
        var replayed = await _context.IncomingNachaProcessingEvents.AsNoTracking().AnyAsync(x =>
            x.IncomingNachaFileIngestionId == queue.IncomingNachaFileIngestionId
            && x.EventType == idempotentEventType
            && x.Message == idempotentMessage
            && x.AchTransactionId == queue.AchTransactionId, ct);

        if (replayed)
        {
            if (tx is not null)
            {
                await tx.CommitAsync(ct);
            }

            return new IncomingNachaManualActionResultDto(queue.Id, ToActionLabel(transitionEvent), queue.QueueStatus, queue.QueueStatus, true, "Solicitud idempotente ya aplicada previamente.");
        }

        var previousStatus = queue.QueueStatus;
        var transition = _stateMachineService.EvaluateDispatchTransition(previousStatus, transitionEvent);
        if (!transition.IsAllowed || !transition.NextStatus.HasValue)
        {
            await RegisterTransitionEventAsync(
                queue,
                normalizedBy,
                transitionEvent,
                idempotentEventType,
                idempotentMessage,
                "Rejected",
                previousStatus,
                previousStatus,
                request,
                transition,
                ct);

            throw new InvalidOperationException($"[{transition.ResultCode}] {transition.Message}");
        }

        queue.QueueStatus = transition.NextStatus.Value;
        var appliedMessage = apply(queue, request);

        await RegisterTransitionEventAsync(
            queue,
            normalizedBy,
            transitionEvent,
            idempotentEventType,
            idempotentMessage,
            "Applied",
            previousStatus,
            queue.QueueStatus,
            request,
            transition,
            ct);

        if (tx is not null)
        {
            await tx.CommitAsync(ct);
        }

        return new IncomingNachaManualActionResultDto(queue.Id, ToActionLabel(transitionEvent), previousStatus, queue.QueueStatus, false, $"{appliedMessage} [{transition.ResultCode}]");
    }

    private IncomingNachaQueueListItemDto ToQueueListDto(QueueProjection x)
    {
        return new IncomingNachaQueueListItemDto(
            x.Id,
            x.IncomingNachaFileIngestionId,
            x.AchTransactionId,
            x.AchCycleId,
            x.ClearingHouseId,
            x.QueueStatus,
            x.Priority,
            x.AttemptCount,
            x.NextAttemptAtUtc,
            x.LastAttemptAtUtc,
            x.LastErrorCode,
            x.LastErrorMessage,
            x.LastResponseCode,
            x.ConfirmedAtUtc,
            x.CreatedAt,
            _stateMachineService.GetAllowedDispatchActions(x.QueueStatus));
    }

    private static string ToActionLabel(IncomingNachaDispatchEvent transitionEvent)
        => transitionEvent switch
        {
            IncomingNachaDispatchEvent.ManualRetry => "Retry",
            IncomingNachaDispatchEvent.ManualUnblock => "Unblock",
            IncomingNachaDispatchEvent.ManualRequeue => "Requeue",
            IncomingNachaDispatchEvent.ManualMarkFailedFinal => "MarkFailedFinal",
            _ => transitionEvent.ToString()
        };

    private async Task RegisterTransitionEventAsync(
        IncomingNachaDispatchQueue queue,
        string normalizedBy,
        IncomingNachaDispatchEvent transitionEvent,
        string eventType,
        string eventMessage,
        string eventStatus,
        IncomingNachaDispatchQueueStatus previousStatus,
        IncomingNachaDispatchQueueStatus currentStatus,
        IncomingNachaManualActionRequest request,
        IncomingNachaDispatchTransitionDecision transition,
        CancellationToken ct)
    {
        _context.IncomingNachaProcessingEvents.Add(new IncomingNachaProcessingEvent
        {
            IncomingNachaFileIngestionId = queue.IncomingNachaFileIngestionId,
            AchTransactionId = queue.AchTransactionId,
            EventType = eventType,
            EventStatus = eventStatus,
            Message = eventMessage,
            EvidenceJson = JsonSerializer.Serialize(new
            {
                queueId = queue.Id,
                action = ToActionLabel(transitionEvent),
                transitionEvent,
                transition.ResultCode,
                transition.Message,
                previousStatus,
                currentStatus,
                request.Justification,
                request.Priority,
                request.IdempotencyKey,
                performedBy = normalizedBy
            }),
            OccurredAtUtc = DateTime.UtcNow,
            RaisedBy = normalizedBy
        });

        await _context.SaveChangesAsync(ct);
    }

    private sealed record QueueProjection(
        Guid Id,
        Guid IncomingNachaFileIngestionId,
        int AchTransactionId,
        string AchCycleId,
        int ClearingHouseId,
        IncomingNachaDispatchQueueStatus QueueStatus,
        int Priority,
        int AttemptCount,
        DateTime? NextAttemptAtUtc,
        DateTime? LastAttemptAtUtc,
        string LastErrorCode,
        string LastErrorMessage,
        string LastResponseCode,
        DateTime? ConfirmedAtUtc,
        DateTimeOffset CreatedAt);

    private static IReadOnlyList<IncomingNachaTimelinePointDto> BuildTimelineWithEmptyBuckets(
        DateTime fromUtc,
        DateTime toUtc,
        IReadOnlyList<IncomingNachaTimelinePointDto> rawTimeline)
    {
        var normalizedFrom = new DateTime(fromUtc.Year, fromUtc.Month, fromUtc.Day, fromUtc.Hour, 0, 0, DateTimeKind.Utc);
        var normalizedTo = new DateTime(toUtc.Year, toUtc.Month, toUtc.Day, toUtc.Hour, 0, 0, DateTimeKind.Utc);
        var map = rawTimeline.ToDictionary(x => x.BucketAtUtc, x => x);
        var results = new List<IncomingNachaTimelinePointDto>();

        for (var bucket = normalizedFrom; bucket <= normalizedTo; bucket = bucket.AddHours(1))
        {
            if (map.TryGetValue(bucket, out var item))
            {
                results.Add(item);
                continue;
            }

            results.Add(new IncomingNachaTimelinePointDto(bucket, 0, 0, 0, 0, 0));
        }

        return results;
    }
}
