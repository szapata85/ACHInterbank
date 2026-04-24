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
            throw new InvalidOperationException($"[{transition.ResultCode}] {transition.Message}");
        }

        queue.QueueStatus = transition.NextStatus.Value;
        var appliedMessage = apply(queue, request);

        _context.IncomingNachaProcessingEvents.Add(new IncomingNachaProcessingEvent
        {
            IncomingNachaFileIngestionId = queue.IncomingNachaFileIngestionId,
            AchTransactionId = queue.AchTransactionId,
            EventType = idempotentEventType,
            EventStatus = "Applied",
            Message = idempotentMessage,
            EvidenceJson = JsonSerializer.Serialize(new
            {
                queueId,
                action = ToActionLabel(transitionEvent),
                transitionEvent,
                transition.ResultCode,
                transition.Message,
                previousStatus,
                currentStatus = queue.QueueStatus,
                request.Justification,
                request.Priority,
                idempotencyKey = normalizedKey,
                performedBy = normalizedBy
            }),
            OccurredAtUtc = DateTime.UtcNow,
            RaisedBy = normalizedBy
        });

        await _context.SaveChangesAsync(ct);
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
}
