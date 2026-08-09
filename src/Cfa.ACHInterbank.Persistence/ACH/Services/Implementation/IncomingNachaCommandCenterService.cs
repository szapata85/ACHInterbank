using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaCommandCenterService : IIncomingNachaCommandCenterService
{
    private readonly AchDbContext _context;
    private readonly IIncomingNachaStateMachineService _stateMachineService;
    private readonly IncomingNachaDispatchResilienceOptions _resilienceOptions;
    private readonly TimeProvider _timeProvider;

    public IncomingNachaCommandCenterService(
        AchDbContext context,
        IIncomingNachaStateMachineService stateMachineService,
        IOptions<IncomingNachaDispatchResilienceOptions>? resilienceOptions = null,
        TimeProvider? timeProvider = null)
    {
        _context = context;
        _stateMachineService = stateMachineService;
        _resilienceOptions = resilienceOptions?.Value ?? new IncomingNachaDispatchResilienceOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<IncomingNachaObservabilitySummaryDto> GetObservabilitySummaryAsync(int windowHours = 24, CancellationToken ct = default)
    {
        var safeWindowHours = Math.Clamp(windowHours, 1, 168);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var fromUtc = nowUtc.AddHours(-safeWindowHours);
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

        var ingestionsByStatusRaw = await ingestionsQ
            .GroupBy(x => x.IngestionStatus)
            .Select(g => new { g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);
        var ingestionsByStatus = ingestionsByStatusRaw
            .Select(x => new IncomingNachaKpiCountDto(x.Key.ToString(), x.Count))
            .ToList();

        var queueByStatusRaw = await queueQ
            .GroupBy(x => x.QueueStatus)
            .Select(g => new { g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);
        var queueByStatus = queueByStatusRaw
            .Select(x => new IncomingNachaKpiCountDto(x.Key.ToString(), x.Count))
            .ToList();

        var queueSnapshot = await queueQ
            .Select(x => new { x.ClearingHouseId, x.AchCycleId, x.QueueStatus, x.LastErrorCode, x.LastAttemptAtUtc })
            .ToListAsync(ct);

        var byClearingCycle = queueSnapshot
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
            .ToList();

        var topErrors = queueSnapshot
            .Where(x => !string.IsNullOrWhiteSpace(x.LastErrorCode))
            .GroupBy(x => x.LastErrorCode)
            .Select(g => new IncomingNachaTopErrorDto(g.Key, g.Count(), g.Max(x => x.LastAttemptAtUtc)))
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

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

        if (query.ClearingHouseId.HasValue)
        {
            q = q.Where(x => x.ResolvedClearingHouseId == query.ClearingHouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.AchCycleId))
        {
            var cycleId = query.AchCycleId.Trim();
            q = q.Where(x => x.ResolvedAchCycleId == cycleId);
        }

        if (query.OperationalDate.HasValue)
        {
            var operationalDate = query.OperationalDate.Value.Date;
            q = q.Where(x => x.OperationalDate.HasValue && x.OperationalDate.Value.Date == operationalDate);
        }

        if (query.UploadedFromUtc.HasValue)
        {
            q = q.Where(x => x.UploadedAtUtc >= query.UploadedFromUtc.Value);
        }

        if (query.UploadedToUtc.HasValue)
        {
            q = q.Where(x => x.UploadedAtUtc <= query.UploadedToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.ResultCode))
        {
            var resultCode = query.ResultCode.Trim();
            q = q.Where(x => _context.IncomingNachaIntegrationExecution.Any(e =>
                e.DispatchQueue.IncomingNachaFileIngestionId == x.Id && e.ResultCode == resultCode));
        }

        if (query.BusinessOutcome.HasValue)
        {
            q = q.Where(x => _context.IncomingNachaIntegrationExecution.Any(e =>
                e.DispatchQueue.IncomingNachaFileIngestionId == x.Id && e.BusinessOutcome == query.BusinessOutcome.Value));
        }

        if (query.HasTechnicalErrors.HasValue)
        {
            q = query.HasTechnicalErrors.Value
                ? q.Where(x => _context.IncomingNachaIntegrationExecution.Any(e => e.DispatchQueue.IncomingNachaFileIngestionId == x.Id && e.IsTechnicalFailure))
                : q.Where(x => !_context.IncomingNachaIntegrationExecution.Any(e => e.DispatchQueue.IncomingNachaFileIngestionId == x.Id && e.IsTechnicalFailure));
        }

        if (query.HasIssues.HasValue)
        {
            q = query.HasIssues.Value
                ? q.Where(x => x.Stage == IncomingNachaIngestionStage.Rejected
                    || x.Stage == IncomingNachaIngestionStage.Failed
                    || _context.IncomingNachaIntegrationExecution.Any(e => e.DispatchQueue.IncomingNachaFileIngestionId == x.Id
                        && (e.IsTechnicalFailure || e.BusinessOutcome == IncomingNachaBusinessOutcome.Rejected || e.BusinessOutcome == IncomingNachaBusinessOutcome.Returned)))
                : q.Where(x => x.Stage != IncomingNachaIngestionStage.Rejected
                    && x.Stage != IncomingNachaIngestionStage.Failed
                    && !_context.IncomingNachaIntegrationExecution.Any(e => e.DispatchQueue.IncomingNachaFileIngestionId == x.Id
                        && (e.IsTechnicalFailure || e.BusinessOutcome == IncomingNachaBusinessOutcome.Rejected || e.BusinessOutcome == IncomingNachaBusinessOutcome.Returned)));
        }

        var total = await q.CountAsync(ct);
        q = query.SortBy.Trim().ToLowerInvariant() switch
        {
            "filename" => query.SortDescending ? q.OrderByDescending(x => x.FileName) : q.OrderBy(x => x.FileName),
            "operationaldate" => query.SortDescending ? q.OrderByDescending(x => x.OperationalDate) : q.OrderBy(x => x.OperationalDate),
            "cycle" => query.SortDescending ? q.OrderByDescending(x => x.ResolvedAchCycleId) : q.OrderBy(x => x.ResolvedAchCycleId),
            _ => query.SortDescending ? q.OrderByDescending(x => x.UploadedAtUtc) : q.OrderBy(x => x.UploadedAtUtc)
        };

        var rows = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                Item = new IncomingNachaIngestionListItemDto(
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
                    _context.IncomingNachaProcessingEvents.Count(e => e.IncomingNachaFileIngestionId == x.Id)),
                x.Stage,
                x.UploadedBy
            })
            .ToListAsync(ct);

        var ingestionIds = rows.Select(x => x.Item.Id).ToArray();
        var clearingHouseIds = rows.Where(x => x.Item.ResolvedClearingHouseId.HasValue)
            .Select(x => x.Item.ResolvedClearingHouseId!.Value).Distinct().ToArray();
        var clearingHouseNames = await _context.ClearingHouses.AsNoTracking()
            .Where(x => clearingHouseIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        var processing = await _context.IncomingNachaFileProcessingResults.AsNoTracking()
            .Where(x => ingestionIds.Contains(x.IncomingNachaFileIngestionId))
            .GroupBy(x => x.IncomingNachaFileIngestionId)
            .Select(g => g.OrderByDescending(x => x.AttemptNumber).Select(x => new
            {
                x.IncomingNachaFileIngestionId,
                x.TotalBatches,
                x.TotalEntries
            }).First())
            .ToListAsync(ct);
        var processingByIngestion = processing.ToDictionary(x => x.IncomingNachaFileIngestionId);
        var financialTotals = await _context.FileControls.AsNoTracking()
            .Where(x => x.NachaHeader != null && x.NachaHeader.IncomingNachaFileIngestionId.HasValue
                && ingestionIds.Contains(x.NachaHeader.IncomingNachaFileIngestionId.Value))
            .GroupBy(x => x.NachaHeader!.IncomingNachaFileIngestionId!.Value)
            .Select(g => new { IngestionId = g.Key, Debit = g.Sum(x => x.TotalDebitAmount), Credit = g.Sum(x => x.TotalCreditAmount) })
            .ToListAsync(ct);
        var totalsByIngestion = financialTotals.ToDictionary(x => x.IngestionId);
        var executionRows = await _context.IncomingNachaIntegrationExecution.AsNoTracking()
            .Where(x => ingestionIds.Contains(x.DispatchQueue.IncomingNachaFileIngestionId))
            .Select(x => new { x.DispatchQueue.IncomingNachaFileIngestionId, x.BusinessOutcome, x.IsTechnicalFailure })
            .ToListAsync(ct);
        var queueRows = await _context.IncomingNachaDispatchQueue.AsNoTracking()
            .Where(x => ingestionIds.Contains(x.IncomingNachaFileIngestionId))
            .Select(x => new { x.IncomingNachaFileIngestionId, x.QueueStatus, x.CreatedAt })
            .ToListAsync(ct);

        var items = rows.Select(x =>
        {
            processingByIngestion.TryGetValue(x.Item.Id, out var processingResult);
            totalsByIngestion.TryGetValue(x.Item.Id, out var totals);
            var itemExecutions = executionRows.Where(e => e.IncomingNachaFileIngestionId == x.Item.Id).ToList();
            var itemQueue = queueRows.Where(qr => qr.IncomingNachaFileIngestionId == x.Item.Id).ToList();
            var hasTechnicalErrors = itemExecutions.Any(e => e.IsTechnicalFailure);
            var hasIssues = hasTechnicalErrors
                || x.Stage is IncomingNachaIngestionStage.Rejected or IncomingNachaIngestionStage.Failed
                || itemExecutions.Any(e => e.BusinessOutcome is IncomingNachaBusinessOutcome.Rejected or IncomingNachaBusinessOutcome.Returned);

            return x.Item with
            {
                IngestionStatusText = IncomingNachaContractText.IngestionStatus(x.Item.IngestionStatus),
                StageCode = x.Stage.ToString(),
                StageText = IncomingNachaContractText.Stage(x.Stage),
                ClearingHouseName = x.Item.ResolvedClearingHouseId.HasValue
                    ? clearingHouseNames.GetValueOrDefault(x.Item.ResolvedClearingHouseId.Value, $"Cámara {x.Item.ResolvedClearingHouseId.Value}")
                    : "Por identificar",
                UploadedBy = string.IsNullOrWhiteSpace(x.UploadedBy) ? "Sistema" : x.UploadedBy,
                TotalBatches = processingResult?.TotalBatches ?? 0,
                TotalTransactions = processingResult?.TotalEntries ?? 0,
                TotalDebit = totals?.Debit ?? 0m,
                TotalCredit = totals?.Credit ?? 0m,
                ProcessingStatusText = FileProcessingStatusText(itemQueue.Select(qr => qr.QueueStatus).ToList(), x.Stage),
                OverallResultText = FileOverallResultText(itemExecutions.Select(e => e.BusinessOutcome).ToList(), hasTechnicalErrors, x.Stage),
                ScheduledAtUtc = itemQueue.Count == 0 ? null : itemQueue.Min(qr => qr.CreatedAt).UtcDateTime,
                HasTechnicalErrors = hasTechnicalErrors,
                HasIssues = hasIssues
            };
        }).ToList();

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

        var latestProcessing = await _context.IncomingNachaFileProcessingResults.AsNoTracking()
            .Where(x => x.IncomingNachaFileIngestionId == ingestionId)
            .OrderByDescending(x => x.AttemptNumber)
            .Select(x => new { x.TotalBatches, x.TotalEntries, x.TotalAddendas })
            .FirstOrDefaultAsync(ct);
        var financialTotals = await _context.FileControls.AsNoTracking()
            .Where(x => x.NachaHeader != null && x.NachaHeader.IncomingNachaFileIngestionId == ingestionId)
            .GroupBy(_ => 1)
            .Select(g => new { Debit = g.Sum(x => x.TotalDebitAmount), Credit = g.Sum(x => x.TotalCreditAmount) })
            .FirstOrDefaultAsync(ct);
        var outcomes = await _context.IncomingNachaIntegrationExecution.AsNoTracking()
            .Where(x => x.DispatchQueue.IncomingNachaFileIngestionId == ingestionId)
            .GroupBy(x => x.BusinessOutcome)
            .Select(g => new { Outcome = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var technicalFailures = await _context.IncomingNachaIntegrationExecution.AsNoTracking()
            .CountAsync(x => x.DispatchQueue.IncomingNachaFileIngestionId == ingestionId && x.IsTechnicalFailure, ct);
        var clearingHouseName = ingestion.ResolvedClearingHouseId.HasValue
            ? await _context.ClearingHouses.AsNoTracking()
                .Where(x => x.Id == ingestion.ResolvedClearingHouseId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(ct)
            : null;
        var pendingTransactions = queue.Items.Count(x => x.QueueStatus is not IncomingNachaDispatchQueueStatus.Confirmed
            and not IncomingNachaDispatchQueueStatus.FailedFinal);
        var detailOutcomes = outcomes.Select(x => x.Outcome).ToList();

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
            events)
        {
            Summary = new IncomingNachaFileSummaryDto(
                latestProcessing?.TotalBatches ?? 0,
                latestProcessing?.TotalEntries ?? 0,
                latestProcessing?.TotalAddendas ?? 0,
                financialTotals?.Debit ?? 0m,
                financialTotals?.Credit ?? 0m,
                outcomes.FirstOrDefault(x => x.Outcome == IncomingNachaBusinessOutcome.Successful)?.Count ?? 0,
                outcomes.FirstOrDefault(x => x.Outcome == IncomingNachaBusinessOutcome.Rejected)?.Count ?? 0,
                outcomes.FirstOrDefault(x => x.Outcome == IncomingNachaBusinessOutcome.Returned)?.Count ?? 0,
                technicalFailures),
            AdmissionIssue = BuildAdmissionIssue(ingestion),
            IngestionStatusText = IncomingNachaContractText.IngestionStatus(ingestion.IngestionStatus),
            StageCode = ingestion.Stage.ToString(),
            StageText = IncomingNachaContractText.Stage(ingestion.Stage),
            ClearingHouseName = clearingHouseName ?? (ingestion.ResolvedClearingHouseId.HasValue ? $"Cámara {ingestion.ResolvedClearingHouseId.Value}" : "Por identificar"),
            UploadedBy = string.IsNullOrWhiteSpace(ingestion.UploadedBy) ? "Sistema" : ingestion.UploadedBy,
            UploadedAtUtc = ingestion.UploadedAtUtc,
            ReceivedAtUtc = ingestion.ReceivedAtUtc,
            OverallResultText = FileOverallResultText(detailOutcomes, technicalFailures > 0, ingestion.Stage),
            PendingTransactions = pendingTransactions
        };
    }

    public async Task<IReadOnlyList<IncomingNachaValidationDto>?> GetIngestionValidationsAsync(
        Guid ingestionId,
        CancellationToken ct = default)
    {
        var ingestion = await _context.IncomingNachaFileIngestions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ingestionId, ct);
        if (ingestion is null)
        {
            return null;
        }

        var issue = BuildAdmissionIssue(ingestion);
        if (issue is not null)
        {
            var persistedIssue = await TryReadPersistedAdmissionIssueAsync(ingestionId, ct) ?? issue;
            return
            [
                new IncomingNachaValidationDto(
                    persistedIssue.Code,
                    persistedIssue.Title,
                    persistedIssue.Message,
                    persistedIssue.ExpectedValue,
                    persistedIssue.FoundValue,
                    persistedIssue.SuggestedAction,
                    persistedIssue.ErrorType,
                    persistedIssue.Severity,
                    false)
                {
                    OccurredAtUtc = ingestion.UploadedAtUtc
                }
            ];
        }

        if (ingestion.Stage is IncomingNachaIngestionStage.Persisted
            || ingestion.Stage is IncomingNachaIngestionStage.Parsing
            || ingestion.Stage is IncomingNachaIngestionStage.ValidatingContent
            || ingestion.Stage is IncomingNachaIngestionStage.Persisting)
        {
            return
            [
                new IncomingNachaValidationDto(
                    "ADMISSION_ACCEPTED",
                    "Archivo admitido",
                    "El archivo superó las validaciones de fecha, cámara, perfil y ciclo.",
                    ingestion.OperationalDate?.ToString("yyyy-MM-dd"),
                    ingestion.HeaderDate?.ToString("yyyy-MM-dd"),
                    "Continúe con el seguimiento del procesamiento.",
                    "Functional",
                    "Information",
                    true)
                {
                    OccurredAtUtc = ingestion.UploadedAtUtc
                }
            ];
        }

        return [];
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
                x.Classification.EntryDetailId,
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
                x.Id, x.DispatchQueueId, x.EntryDetailId, x.AttemptNumber, x.MethodName, x.CorrelationId,
                x.ProcessingStatus,
                x.ProcessingStatus == IncomingNachaIndividualProcessingStatus.Completed ? "Procesado"
                    : x.ProcessingStatus == IncomingNachaIndividualProcessingStatus.RetryPending ? "Pendiente de reintento"
                    : x.ProcessingStatus == IncomingNachaIndividualProcessingStatus.TechnicalFailed ? "Error técnico"
                    : x.ProcessingStatus == IncomingNachaIndividualProcessingStatus.Processing ? "Procesando"
                    : x.ProcessingStatus == IncomingNachaIndividualProcessingStatus.Scheduled ? "Programado"
                    : "Pendiente",
                x.BusinessOutcome,
                x.BusinessOutcome == IncomingNachaBusinessOutcome.Successful ? "Exitoso"
                    : x.BusinessOutcome == IncomingNachaBusinessOutcome.Rejected ? "Rechazado"
                    : x.BusinessOutcome == IncomingNachaBusinessOutcome.Returned ? "Devuelto"
                    : x.BusinessOutcome == IncomingNachaBusinessOutcome.NotProcessed ? "No procesado"
                    : "Pendiente de respuesta",
                x.AchReturnCodeId, x.ResultCode, x.ResultDescription,
                x.IsSuccess, x.IsRetryable, x.StartedAtUtc, x.FinishedAtUtc)
            {
                LogicalEndpoint = string.IsNullOrEmpty(x.SoapEndpoint) ? string.Empty : "WSCFAACH",
                DurationMs = x.DurationMs,
                TransportStatusCode = x.TransportStatus.ToString(),
                TransportStatusText = IncomingNachaContractText.TransportStatus(x.TransportStatus),
                TechnicalErrorCode = x.TechnicalErrorCode,
                TechnicalErrorMessage = x.TechnicalErrorMessage,
                ResultSource = x.ResultSource,
                ExternalTransactionId = x.ExternalTransactionId,
                ProcessedAtUtc = x.ProcessedAtUtc
            })
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
            _stateMachineService.GetAllowedDispatchActions(queue.QueueStatus))
        {
            EntryDetailId = queue.Classification.EntryDetailId,
            QueueStatusText = IncomingNachaContractText.QueueStatus(queue.QueueStatus),
            ScheduledAtUtc = queue.CreatedAt.UtcDateTime,
            MaxAttempts = _resilienceOptions.MaxAttempts,
            SoapOperation = executions.FirstOrDefault()?.MethodName ?? "Proc_Transacciones"
        };

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
            0)
        {
            IngestionStatusText = IncomingNachaContractText.IngestionStatus(ingestion.IngestionStatus),
            StageCode = ingestion.Stage.ToString(),
            StageText = IncomingNachaContractText.Stage(ingestion.Stage)
        };

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

    public async Task<IncomingNachaPageResult<IncomingNachaBatchDto>> GetBatchesAsync(
        Guid ingestionId,
        IncomingNachaBatchQuery query,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var rows = _context.BatchHeaders.AsNoTracking()
            .Where(x => x.NachaHeader != null && x.NachaHeader.IncomingNachaFileIngestionId == ingestionId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            rows = rows.Where(x => (x.CompanyName ?? string.Empty).Contains(search)
                || x.BatchNumber.ToString().Contains(search));
        }

        rows = query.SortBy.Trim().ToLowerInvariant() switch
        {
            "companyname" => query.SortDescending ? rows.OrderByDescending(x => x.CompanyName) : rows.OrderBy(x => x.CompanyName),
            _ => query.SortDescending ? rows.OrderByDescending(x => x.BatchNumber) : rows.OrderBy(x => x.BatchNumber)
        };

        var total = await rows.CountAsync(ct);
        var items = await rows.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new IncomingNachaBatchDto(
                x.BatchID,
                x.BatchNumber,
                x.CompanyName ?? string.Empty,
                x.ServiceClassCode ?? string.Empty,
                x.StandardEntryClassCode ?? string.Empty,
                x.EffectiveEntryDate,
                x.EntryDetails.Count,
                x.EntryDetails.Sum(e => e.Amount ?? 0m),
                x.BatchControl == null ? 0m : x.BatchControl.TotalDebitAmount,
                x.BatchControl == null ? 0m : x.BatchControl.TotalCreditAmount)
            {
                CompanyEntryDescription = x.CompanyEntryDescription ?? string.Empty
            })
            .ToListAsync(ct);

        return new IncomingNachaPageResult<IncomingNachaBatchDto>(items, page, pageSize, total);
    }

    public async Task<IncomingNachaPageResult<IncomingNachaTransactionDto>> GetTransactionsAsync(
        Guid ingestionId,
        IncomingNachaTransactionQuery query,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var rows = _context.EntryDetails.AsNoTracking()
            .Where(x => x.NachaHeader != null && x.NachaHeader.IncomingNachaFileIngestionId == ingestionId);

        if (query.BatchId.HasValue) rows = rows.Where(x => x.BatchHeaderId == query.BatchId.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            rows = rows.Where(x => (x.SequenceNumber ?? string.Empty).Contains(search)
                || (x.RecipUserName ?? string.Empty).Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.ResultCode))
        {
            var resultCode = query.ResultCode.Trim();
            rows = rows.Where(x => _context.IncomingNachaIntegrationExecution
                .Any(e => e.EntryDetailId == x.EntryDetailID && e.ResultCode == resultCode));
        }
        if (query.BusinessOutcome.HasValue)
            rows = rows.Where(x => _context.IncomingNachaIntegrationExecution.Any(e => e.EntryDetailId == x.EntryDetailID && e.BusinessOutcome == query.BusinessOutcome.Value));
        if (query.ProcessingStatus.HasValue)
            rows = rows.Where(x => _context.IncomingNachaIntegrationExecution.Any(e => e.EntryDetailId == x.EntryDetailID && e.ProcessingStatus == query.ProcessingStatus.Value));
        if (query.HasAddenda.HasValue)
            rows = query.HasAddenda.Value
                ? rows.Where(x => _context.AddendaRecords.Any(a => a.EntryDetailId == x.EntryDetailID))
                : rows.Where(x => !_context.AddendaRecords.Any(a => a.EntryDetailId == x.EntryDetailID));
        if (query.HasTechnicalError.HasValue)
            rows = query.HasTechnicalError.Value
                ? rows.Where(x => _context.IncomingNachaIntegrationExecution.Any(e => e.EntryDetailId == x.EntryDetailID && e.IsTechnicalFailure))
                : rows.Where(x => !_context.IncomingNachaIntegrationExecution.Any(e => e.EntryDetailId == x.EntryDetailID && e.IsTechnicalFailure));
        if (!string.IsNullOrWhiteSpace(query.TransactionCode))
        {
            var transactionCode = query.TransactionCode.Trim();
            rows = rows.Where(x => x.TransactionCode == transactionCode);
        }

        rows = query.SortBy.Trim().ToLowerInvariant() switch
        {
            "amount" => query.SortDescending ? rows.OrderByDescending(x => x.Amount) : rows.OrderBy(x => x.Amount),
            "batchnumber" => query.SortDescending ? rows.OrderByDescending(x => x.BatchNumber) : rows.OrderBy(x => x.BatchNumber),
            _ => query.SortDescending ? rows.OrderByDescending(x => x.SequenceNumber) : rows.OrderBy(x => x.SequenceNumber)
        };

        var total = await rows.CountAsync(ct);
        var entries = await rows.Include(x => x.BatchHeader)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var entryIds = entries.Select(x => x.EntryDetailID).ToArray();
        var classifications = await _context.IncomingNachaEntryClassifications.AsNoTracking()
            .Where(x => x.IncomingNachaFileIngestionId == ingestionId && entryIds.Contains(x.EntryDetailId))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);
        var classificationByEntry = classifications.GroupBy(x => x.EntryDetailId).ToDictionary(x => x.Key, x => x.First());
        var classificationIds = classifications.Select(x => x.Id).ToArray();
        var queues = await _context.IncomingNachaDispatchQueue.AsNoTracking()
            .Where(x => classificationIds.Contains(x.IncomingNachaEntryClassificationId))
            .ToListAsync(ct);
        var queueByClassification = queues.GroupBy(x => x.IncomingNachaEntryClassificationId).ToDictionary(x => x.Key, x => x.OrderByDescending(q => q.CreatedAt).First());
        var executions = await _context.IncomingNachaIntegrationExecution.AsNoTracking()
            .Where(x => x.EntryDetailId.HasValue && entryIds.Contains(x.EntryDetailId.Value))
            .OrderByDescending(x => x.AttemptNumber)
            .ToListAsync(ct);
        var executionByEntry = executions.GroupBy(x => x.EntryDetailId!.Value).ToDictionary(x => x.Key, x => x.First());
        var addendaCounts = await _context.AddendaRecords.AsNoTracking()
            .Where(x => x.EntryDetailId.HasValue && entryIds.Contains(x.EntryDetailId.Value))
            .GroupBy(x => x.EntryDetailId!.Value)
            .Select(x => new { EntryDetailId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.EntryDetailId, x => x.Count, ct);
        var transactionCodes = entries.Select(x => x.TransactionCode).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray();
        var transactionCodeDescriptions = await _context.TransactionCodes.AsNoTracking()
            .Where(x => transactionCodes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, x => x.Description, ct);

        var items = entries.Select(entry =>
        {
            classificationByEntry.TryGetValue(entry.EntryDetailID, out var classification);
            IncomingNachaDispatchQueue? queue = null;
            if (classification is not null) queueByClassification.TryGetValue(classification.Id, out queue);
            executionByEntry.TryGetValue(entry.EntryDetailID, out var execution);
            var transactionCodeDescription = ResolveTransactionCodeDescription(
                entry.TransactionCode,
                transactionCodeDescriptions);
            return new IncomingNachaTransactionDto(
                entry.EntryDetailID,
                entry.BatchHeaderId ?? 0,
                entry.BatchNumber,
                entry.SequenceNumber ?? string.Empty,
                entry.TransactionCode ?? string.Empty,
                entry.Amount ?? 0m,
                addendaCounts.GetValueOrDefault(entry.EntryDetailID),
                classification?.FunctionalClass.ToString() ?? "Pending",
                ClassificationText(classification?.FunctionalClass),
                queue?.QueueStatus.ToString() ?? "Pending",
                DispatchStatusText(queue?.QueueStatus),
                queue?.AttemptCount ?? 0,
                execution?.ProcessingStatus,
                ProcessingStatusText(execution?.ProcessingStatus),
                execution?.BusinessOutcome,
                BusinessOutcomeText(execution?.BusinessOutcome),
                execution?.ResultCode ?? string.Empty,
                execution?.ResultDescription ?? string.Empty,
                execution?.ProcessedAtUtc,
                execution?.CorrelationId ?? ingestionId.ToString("N"))
            {
                DispatchQueueId = queue?.Id,
                ClearingHouseId = queue?.ClearingHouseId ?? 0,
                OperationalDate = queue?.OperationalDate,
                AchCycleId = queue?.AchCycleId ?? string.Empty,
                ScheduledAtUtc = queue?.CreatedAt.UtcDateTime,
                StartedAtUtc = execution?.StartedAtUtc,
                FinishedAtUtc = execution?.FinishedAtUtc,
                NextRetryAtUtc = queue?.NextAttemptAtUtc,
                MaxAttempts = _resilienceOptions.MaxAttempts,
                SoapOperation = execution?.MethodName ?? (queue is null ? string.Empty : "Proc_Transacciones"),
                ExternalTransactionId = execution?.ExternalTransactionId ?? string.Empty,
                AchReturnCodeId = execution?.AchReturnCodeId,
                TechnicalErrorCode = execution?.TechnicalErrorCode ?? string.Empty,
                TechnicalErrorMessage = execution?.TechnicalErrorMessage ?? string.Empty,
                TransactionCodeDescription = transactionCodeDescription,
                AccountNumberMasked = MaskValue(entry.AccountNumber, 4),
                OriginInstitution = MaskInstitution(entry.BatchHeader?.OriginParticipantEntityCode),
                DestinationInstitution = MaskInstitution(entry.ReceivingParticipantEntityCode),
                RecipientNameMasked = MaskName(entry.RecipUserName),
                EffectiveEntryDate = entry.BatchHeader?.EffectiveEntryDate ?? string.Empty
            };
        }).ToList();

        return new IncomingNachaPageResult<IncomingNachaTransactionDto>(items, page, pageSize, total);
    }

    private static string ResolveTransactionCodeDescription(
        string? transactionCode,
        IReadOnlyDictionary<string, string?> transactionCodeDescriptions)
    {
        const string fallbackDescription = "Código de transacción NACHA-M";

        if (string.IsNullOrWhiteSpace(transactionCode)
            || !transactionCodeDescriptions.TryGetValue(transactionCode, out var description)
            || string.IsNullOrWhiteSpace(description))
        {
            return fallbackDescription;
        }

        return description;
    }

    public async Task<IReadOnlyList<IncomingNachaAddendaDto>> GetAddendasAsync(
        Guid ingestionId,
        int entryDetailId,
        CancellationToken ct = default)
    {
        var addendas = await _context.AddendaRecords.AsNoTracking()
            .Where(x => x.EntryDetailId == entryDetailId
                && x.NachaHeader != null
                && x.NachaHeader.IncomingNachaFileIngestionId == ingestionId)
            .OrderBy(x => x.AddendumSequence)
            .Select(x => new
            {
                x.AddendaID,
                x.CodeTypeAddendumRecord,
                x.AddendumSequence,
                x.ReturnReasonCode,
                x.OriginalTraceNumber,
                x.PaymentRelatedInformation
            })
            .ToListAsync(ct);

        return addendas.Select(x => new IncomingNachaAddendaDto(
            x.AddendaID,
            x.CodeTypeAddendumRecord ?? string.Empty,
            x.AddendumSequence ?? string.Empty,
            x.ReturnReasonCode ?? string.Empty,
            MaskValue(x.OriginalTraceNumber, 4),
            MaskText(x.PaymentRelatedInformation))).ToList();
    }

    private static string ClassificationText(IncomingNachaFunctionalClass? value) => value switch
    {
        IncomingNachaFunctionalClass.CreditoEntrante => "Crédito entrante",
        IncomingNachaFunctionalClass.DebitoEntrante => "Débito entrante",
        IncomingNachaFunctionalClass.Prenotificacion => "Prenotificación",
        IncomingNachaFunctionalClass.Devolucion => "Devolución",
        IncomingNachaFunctionalClass.NoProcesable => "No procesable",
        IncomingNachaFunctionalClass.Ambigua => "Requiere revisión",
        IncomingNachaFunctionalClass.Inconsistente => "Información inconsistente",
        _ => "Pendiente de clasificación"
    };

    private static string DispatchStatusText(IncomingNachaDispatchQueueStatus? value) => value switch
    {
        IncomingNachaDispatchQueueStatus.Queued => "Pendiente de programación",
        IncomingNachaDispatchQueueStatus.Dispatching => "Enviando al servicio",
        IncomingNachaDispatchQueueStatus.Dispatched => "Enviado al servicio",
        IncomingNachaDispatchQueueStatus.Confirmed => "Procesado",
        IncomingNachaDispatchQueueStatus.RetryPending => "Pendiente de reintento",
        IncomingNachaDispatchQueueStatus.FailedFinal => "Falló el procesamiento",
        IncomingNachaDispatchQueueStatus.Blocked => "Bloqueado",
        IncomingNachaDispatchQueueStatus.WaitingWindow => "En espera de ventana",
        _ => "Pendiente de programación"
    };

    private static string ProcessingStatusText(IncomingNachaIndividualProcessingStatus? value) => value switch
    {
        IncomingNachaIndividualProcessingStatus.Scheduled => "Programado",
        IncomingNachaIndividualProcessingStatus.Processing => "Procesando",
        IncomingNachaIndividualProcessingStatus.Completed => "Procesado",
        IncomingNachaIndividualProcessingStatus.RetryPending => "Pendiente de reintento",
        IncomingNachaIndividualProcessingStatus.TechnicalFailed => "Error técnico",
        _ => "Pendiente"
    };

    private static string BusinessOutcomeText(IncomingNachaBusinessOutcome? value) => value switch
    {
        IncomingNachaBusinessOutcome.Successful => "Exitoso",
        IncomingNachaBusinessOutcome.Rejected => "Rechazado",
        IncomingNachaBusinessOutcome.Returned => "Devuelto",
        IncomingNachaBusinessOutcome.NotProcessed => "No procesado",
        _ => "Pendiente de respuesta"
    };

    private static string FileProcessingStatusText(
        IReadOnlyCollection<IncomingNachaDispatchQueueStatus> statuses,
        IncomingNachaIngestionStage stage)
    {
        if (stage == IncomingNachaIngestionStage.Failed) return "Error técnico";
        if (stage == IncomingNachaIngestionStage.Rejected) return "No procesado";
        if (statuses.Count == 0) return stage == IncomingNachaIngestionStage.Persisted ? "Pendiente de programación" : "Carga en curso";
        if (statuses.All(x => x == IncomingNachaDispatchQueueStatus.Confirmed)) return "Procesado";
        if (statuses.Any(x => x is IncomingNachaDispatchQueueStatus.Dispatching or IncomingNachaDispatchQueueStatus.Dispatched)) return "Procesando";
        if (statuses.Any(x => x == IncomingNachaDispatchQueueStatus.RetryPending)) return "Pendiente de reintento";
        if (statuses.Any(x => x == IncomingNachaDispatchQueueStatus.FailedFinal)) return "Procesado con errores técnicos";
        if (statuses.Any(x => x == IncomingNachaDispatchQueueStatus.Blocked)) return "Procesamiento bloqueado";
        return "Programado";
    }

    private static string FileOverallResultText(
        IReadOnlyCollection<IncomingNachaBusinessOutcome> outcomes,
        bool hasTechnicalErrors,
        IncomingNachaIngestionStage stage)
    {
        if (stage == IncomingNachaIngestionStage.Rejected) return "Rechazado durante la validación";
        if (hasTechnicalErrors) return "Con errores técnicos";
        if (outcomes.Count == 0) return "Pendiente";
        if (outcomes.Any(x => x is IncomingNachaBusinessOutcome.Rejected or IncomingNachaBusinessOutcome.Returned)) return "Procesado con novedades";
        if (outcomes.All(x => x == IncomingNachaBusinessOutcome.Successful)) return "Procesado correctamente";
        return "Procesamiento pendiente";
    }

    private static string MaskValue(string? value, int visibleSuffix)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0) return string.Empty;
        var suffixLength = Math.Min(visibleSuffix, normalized.Length);
        return $"****{normalized[^suffixLength..]}";
    }

    private static string MaskInstitution(string? value) => MaskValue(value, 4);

    private static string MaskName(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0) return string.Empty;
        return normalized.Length == 1 ? "*" : $"{normalized[0]}***";
    }

    private static string MaskText(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0) return string.Empty;
        return normalized.Length <= 8 ? "Información protegida" : $"Información protegida · …{normalized[^4..]}";
    }

    public async Task<IncomingNachaPageResult<IncomingNachaOrphanDto>> GetOrphansAsync(
        IncomingNachaOrphanQuery query,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var rows = _context.IncomingNachaTransactionLinks.AsNoTracking()
            .Where(x => !x.IsFinal
                && (x.LinkType == IncomingNachaLinkType.NotFound
                    || x.LinkType == IncomingNachaLinkType.Ambiguous
                    || x.LinkType == IncomingNachaLinkType.NoResuelto));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            rows = rows.Where(x => x.Ingestion.FileName.Contains(search)
                || (x.EntryDetail != null && (x.EntryDetail.SequenceNumber ?? string.Empty).Contains(search))
                || (x.AddendaRecord != null && (x.AddendaRecord.OriginalTraceNumber ?? string.Empty).Contains(search))
                || (x.AddendaRecord != null && (x.AddendaRecord.ReturnReasonCode ?? string.Empty).Contains(search)));
        }

        var total = await rows.CountAsync(ct);
        var links = await rows
            .Include(x => x.Ingestion)
            .Include(x => x.EntryDetail).ThenInclude(x => x!.BatchHeader)
            .Include(x => x.AddendaRecord)
            .OrderBy(x => x.LinkedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new IncomingNachaPageResult<IncomingNachaOrphanDto>(
            await BuildOrphanDtosAsync(links, ct), page, pageSize, total);
    }

    public async Task<IncomingNachaOrphanDto?> GetOrphanAsync(Guid linkId, CancellationToken ct = default)
    {
        var link = await _context.IncomingNachaTransactionLinks.AsNoTracking()
            .Include(x => x.Ingestion)
            .Include(x => x.EntryDetail).ThenInclude(x => x!.BatchHeader)
            .Include(x => x.AddendaRecord)
            .FirstOrDefaultAsync(x => x.Id == linkId, ct);
        if (link is null)
        {
            return null;
        }

        return (await BuildOrphanDtosAsync([link], ct)).Single();
    }

    public async Task<IReadOnlyList<IncomingNachaOrphanCandidateDto>> GetOrphanCandidatesAsync(
        Guid linkId,
        string? search,
        CancellationToken ct = default)
    {
        var link = await _context.IncomingNachaTransactionLinks.AsNoTracking()
            .Include(x => x.Ingestion)
            .Include(x => x.EntryDetail)
            .FirstOrDefaultAsync(x => x.Id == linkId, ct)
            ?? throw new KeyNotFoundException("No existe la devolución huérfana indicada.");
        if (link.EntryDetail is null)
        {
            return [];
        }

        var classification = await _context.IncomingNachaEntryClassifications.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IncomingNachaFileIngestionId == link.IncomingNachaFileIngestionId
                && x.EntryDetailId == link.EntryDetailId
                && x.AddendaRecordId == link.AddendaRecordId, ct)
            ?? throw new InvalidOperationException("La devolución no conserva su clasificación funcional.");
        var candidateIds = ExtractOrphanCandidateIds(link.EvidenceJson);
        var account = link.EntryDetail.AccountNumber?.Trim();
        var candidates = _context.AchTransactions.AsNoTracking()
            .Include(x => x.AchCycle)
            .Where(x => x.Amount == link.EntryDetail.Amount.GetValueOrDefault());

        if (link.Ingestion.ResolvedClearingHouseId.HasValue)
        {
            var clearingHouseId = link.Ingestion.ResolvedClearingHouseId.Value;
            candidates = candidates.Where(x => x.AchCycle.ClearingHouseId == clearingHouseId);
        }
        if (!string.IsNullOrWhiteSpace(account))
        {
            candidates = candidates.Where(x => x.DestinationAccountNumber == account);
        }
        if (candidateIds.Length > 0)
        {
            candidates = candidates.Where(x => candidateIds.Contains(x.Id));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            var hasId = int.TryParse(normalizedSearch, out var transactionId);
            candidates = candidates.Where(x => (hasId && x.Id == transactionId)
                || x.TraceNumber.Contains(normalizedSearch)
                || x.TransactionExternalId.Contains(normalizedSearch));
        }

        var transactions = await candidates
            .OrderByDescending(x => x.TraceNumber == classification.OriginalTraceRef)
            .ThenByDescending(x => x.EffectiveEntryDate)
            .Take(20)
            .ToListAsync(ct);

        return transactions.Select(transaction =>
        {
            var incompatibilities = IncomingNachaOrphanCompatibilityPolicy.Evaluate(
                link.Ingestion, link.EntryDetail, classification, transaction, candidateIds);
            return new IncomingNachaOrphanCandidateDto(
                transaction.Id,
                transaction.TraceNumber,
                transaction.Amount,
                transaction.EffectiveEntryDate,
                transaction.State.ToString(),
                MaskValue(transaction.DestinationAccountNumber, 4),
                MaskInstitution(transaction.OriginatingDFI),
                MaskInstitution(transaction.ReceivingDFI),
                incompatibilities.Count == 0,
                incompatibilities);
        }).ToList();
    }

    private async Task<IReadOnlyList<IncomingNachaOrphanDto>> BuildOrphanDtosAsync(
        IReadOnlyList<IncomingNachaTransactionLink> links,
        CancellationToken ct)
    {
        if (links.Count == 0)
        {
            return [];
        }

        var ingestionIds = links.Select(x => x.IncomingNachaFileIngestionId).Distinct().ToArray();
        var entryIds = links.Where(x => x.EntryDetailId.HasValue).Select(x => x.EntryDetailId!.Value).Distinct().ToArray();
        var classifications = await _context.IncomingNachaEntryClassifications.AsNoTracking()
            .Where(x => ingestionIds.Contains(x.IncomingNachaFileIngestionId) && entryIds.Contains(x.EntryDetailId))
            .ToListAsync(ct);
        var classificationByEntry = classifications
            .GroupBy(x => new { x.IncomingNachaFileIngestionId, x.EntryDetailId, x.AddendaRecordId })
            .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.CreatedAt).First());
        var reasonCodes = classifications.Select(x => x.ReturnReasonCode)
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray();
        var clearingHouseIds = links.Where(x => x.Ingestion.ResolvedClearingHouseId.HasValue)
            .Select(x => x.Ingestion.ResolvedClearingHouseId!.Value).Distinct().ToArray();
        var descriptions = await _context.AchReturnCodes.AsNoTracking()
            .Where(x => clearingHouseIds.Contains(x.ClearingHouseId) && reasonCodes.Contains(x.Code) && x.IsActive)
            .OrderByDescending(x => x.EffectiveFrom)
            .ToListAsync(ct);

        return links.Select(link =>
        {
            IncomingNachaEntryClassification? classification = null;
            if (link.EntryDetailId.HasValue)
            {
                classificationByEntry.TryGetValue(new
                {
                    link.IncomingNachaFileIngestionId,
                    EntryDetailId = link.EntryDetailId.Value,
                    link.AddendaRecordId
                }, out classification);
            }
            var returnCode = classification?.ReturnReasonCode ?? link.AddendaRecord?.ReturnReasonCode ?? string.Empty;
            var description = descriptions.FirstOrDefault(x => x.ClearingHouseId == link.Ingestion.ResolvedClearingHouseId
                && string.Equals(x.Code, returnCode, StringComparison.OrdinalIgnoreCase))?.Description ?? string.Empty;
            return new IncomingNachaOrphanDto(
                link.Id,
                link.IncomingNachaFileIngestionId,
                link.Ingestion.FileName,
                link.Ingestion.ReceivedAtUtc ?? link.LinkedAtUtc,
                link.Ingestion.ResolvedClearingHouseId,
                link.Ingestion.ResolvedAchCycleId ?? string.Empty,
                link.Ingestion.OperationalDate,
                link.EntryDetailId ?? 0,
                link.AddendaRecordId,
                link.EntryDetail?.Amount ?? 0m,
                link.EntryDetail?.SequenceNumber ?? string.Empty,
                classification?.OriginalTraceRef ?? link.AddendaRecord?.OriginalTraceNumber ?? string.Empty,
                returnCode,
                description,
                MaskValue(link.EntryDetail?.AccountNumber, 4),
                MaskName(link.EntryDetail?.RecipUserName),
                MaskInstitution(link.EntryDetail?.BatchHeader?.OriginParticipantEntityCode),
                MaskInstitution(link.EntryDetail?.ReceivingParticipantEntityCode),
                link.LinkType,
                ExtractOrphanCandidateIds(link.EvidenceJson),
                link.AchTransactionId,
                link.IsFinal ? "Resuelta" : "Sin relación",
                link.IsFinal ? link.LinkedBy : string.Empty,
                link.IsFinal ? link.LinkedAtUtc : null);
        }).ToList();
    }

    private static int[] ExtractOrphanCandidateIds(string? evidenceJson)
    {
        if (string.IsNullOrWhiteSpace(evidenceJson))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(evidenceJson);
            var root = document.RootElement;
            if (!root.TryGetProperty("candidateTransactionIds", out var values)
                || values.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return values.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.Number && x.TryGetInt32(out _))
                .Select(x => x.GetInt32())
                .Distinct()
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public Task<IncomingNachaManualActionResultDto> RetryManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default)
        => ApplyManualActionAsync(queueId, request, performedBy, IncomingNachaDispatchEvent.ManualRetry, (q, req) =>
        {
            q.NextAttemptAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            q.LastErrorCode = string.Empty;
            q.LastErrorMessage = string.Empty;
            if (req.Priority.HasValue)
            {
                q.Priority = Math.Clamp(req.Priority.Value, 1, 999);
            }

            return "Reintento manual aplicado.";
        }, ct);

    public Task<IncomingNachaManualActionResultDto> UnblockManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default)
        => ApplyManualActionAsync(queueId, request, performedBy, IncomingNachaDispatchEvent.ManualUnblock, (q, req) =>
        {
            q.NextAttemptAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            q.LastErrorCode = string.Empty;
            q.LastErrorMessage = string.Empty;
            if (req.Priority.HasValue)
            {
                q.Priority = Math.Clamp(req.Priority.Value, 1, 999);
            }

            return "Desbloqueo manual aplicado.";
        }, ct);

    public Task<IncomingNachaManualActionResultDto> RequeueManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default)
        => ApplyManualActionAsync(queueId, request, performedBy, IncomingNachaDispatchEvent.ManualRequeue, (q, req) =>
        {
            q.NextAttemptAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            if (req.Priority.HasValue)
            {
                q.Priority = Math.Clamp(req.Priority.Value, 1, 999);
            }

            return "Nueva programación manual aplicada.";
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
            return new IncomingNachaManualActionResultDto(queue.Id, ToActionLabel(transitionEvent), queue.QueueStatus, queue.QueueStatus, true, "Solicitud idempotente ya aplicada previamente.")
            {
                ActionText = ToActionText(transitionEvent)
            };
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

        return new IncomingNachaManualActionResultDto(queue.Id, ToActionLabel(transitionEvent), previousStatus, queue.QueueStatus, false, $"{appliedMessage} [{transition.ResultCode}]")
        {
            ActionText = ToActionText(transitionEvent)
        };
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
            _stateMachineService.GetAllowedDispatchActions(x.QueueStatus))
        {
            EntryDetailId = x.EntryDetailId,
            QueueStatusText = IncomingNachaContractText.QueueStatus(x.QueueStatus),
            ScheduledAtUtc = x.CreatedAt.UtcDateTime,
            MaxAttempts = _resilienceOptions.MaxAttempts,
            SoapOperation = "Proc_Transacciones"
        };
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

    private static string ToActionText(IncomingNachaDispatchEvent transitionEvent)
        => transitionEvent switch
        {
            IncomingNachaDispatchEvent.ManualRetry => "Reintentar",
            IncomingNachaDispatchEvent.ManualUnblock => "Desbloquear",
            IncomingNachaDispatchEvent.ManualRequeue => "Volver a programar",
            IncomingNachaDispatchEvent.ManualMarkFailedFinal => "Marcar como falla final",
            _ => "Acción operativa"
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
            OccurredAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            RaisedBy = normalizedBy
        });

        await _context.SaveChangesAsync(ct);
    }

    private static IncomingNachaAdmissionIssue? BuildAdmissionIssue(IncomingNachaFileIngestion ingestion)
    {
        if (string.IsNullOrWhiteSpace(ingestion.RejectionCode)
            && ingestion.Stage is not IncomingNachaIngestionStage.Rejected)
        {
            return null;
        }

        var technical = !string.IsNullOrWhiteSpace(ingestion.TechnicalErrorCode);
        return new IncomingNachaAdmissionIssue(
            ingestion.RejectionCode ?? ingestion.TechnicalErrorCode ?? "FILE_ADMISSION_REJECTED",
            ingestion.RejectionTitle ?? (technical ? "No fue posible procesar el archivo" : "No fue posible admitir el archivo"),
            ingestion.RejectionDescription ?? ingestion.TechnicalErrorMessage ?? "El archivo no superó las validaciones operativas.",
            ingestion.SuggestedAction ?? "Verifique los datos del archivo y vuelva a intentarlo.",
            technical ? "Technical" : "Functional",
            "Error");
    }

    private async Task<IncomingNachaAdmissionIssue?> TryReadPersistedAdmissionIssueAsync(Guid ingestionId, CancellationToken ct)
    {
        var json = await _context.IncomingNachaFileProcessingResults.AsNoTracking()
            .Where(x => x.IncomingNachaFileIngestionId == ingestionId)
            .OrderByDescending(x => x.AttemptNumber)
            .Select(x => x.ParserErrorsJson)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<IncomingNachaAdmissionIssue[]>(json)?.FirstOrDefault();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record QueueProjection(
        Guid Id,
        Guid IncomingNachaFileIngestionId,
        int EntryDetailId,
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
