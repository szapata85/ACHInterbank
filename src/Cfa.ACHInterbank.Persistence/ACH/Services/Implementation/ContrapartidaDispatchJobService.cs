using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class ContrapartidaDispatchJobService : IContrapartidaDispatchJobService
{
    private const int MaxAttempts = 5;
    private static readonly ContrapartidaDispatchItemStateEnum[] EligibleStates =
    [
        ContrapartidaDispatchItemStateEnum.PendingContrapartidaReport,
        ContrapartidaDispatchItemStateEnum.QueuedForContrapartida,
        ContrapartidaDispatchItemStateEnum.RetryPending
    ];

    private readonly AchDbContext _context;
    private readonly IWscfaachSoapClient _soapClient;
    private readonly IProcContrapartidasRequestMapper _procContrapartidasRequestMapper;
    private readonly IProcContrapartidasResponseParser _responseParser;
    private readonly ILogger<ContrapartidaDispatchJobService> _logger;

    public ContrapartidaDispatchJobService(
        AchDbContext context,
        IWscfaachSoapClient soapClient,
        IProcContrapartidasRequestMapper procContrapartidasRequestMapper,
        IProcContrapartidasResponseParser responseParser,
        ILogger<ContrapartidaDispatchJobService> logger)
    {
        _context = context;
        _soapClient = soapClient;
        _procContrapartidasRequestMapper = procContrapartidasRequestMapper;
        _responseParser = responseParser;
        _logger = logger;
    }

    public async Task<ContrapartidaCycleDispatchResult> ProcessCycleAsync(
        string cycleId,
        int clearingHouseId,
        string triggeredBy,
        int chunkSize,
        CancellationToken ct = default)
    {
        chunkSize = Math.Clamp(chunkSize, 10, 2000);
        var safeTriggeredBy = string.IsNullOrWhiteSpace(triggeredBy) ? "quartz:contrapartida" : triggeredBy.Trim();

        var cycle = await _context.AchCycles
            .AsNoTracking()
            .Include(c => c.ClearingHouse)
            .Include(c => c.ClearingHouseCycleConfig)
            .FirstOrDefaultAsync(c => c.Id == cycleId && c.ClearingHouseId == clearingHouseId, ct)
            ?? throw new InvalidOperationException($"No existe ciclo {cycleId} para cámara {clearingHouseId}.");

        ValidateCycleOperationalWindow(cycle, DateTime.Now);

        var processed = 0;
        var succeeded = 0;
        var failed = 0;
        var partial = 0;
        var chunks = 0;

        while (!ct.IsCancellationRequested)
        {
            var pendingItemIds = await _context.ContrapartidaDispatchItems
                .AsNoTracking()
                .Where(i => i.AchCycleId == cycleId && i.ClearingHouseId == clearingHouseId)
                .Where(i => EligibleStates.Contains(i.State))
                .Where(i => !i.NextAttemptAtUtc.HasValue || i.NextAttemptAtUtc <= DateTime.UtcNow)
                .OrderBy(i => i.Id)
                .Select(i => i.Id)
                .Take(chunkSize)
                .ToListAsync(ct);

            if (pendingItemIds.Count == 0)
            {
                break;
            }

            chunks++;
            var chunkPartialCount = 0;
            var operationToken = $"scheduled:{Guid.NewGuid():N}";

            var batch = new ContrapartidaDispatchBatch
            {
                AchCycleId = cycle.Id,
                ClearingHouseId = cycle.ClearingHouseId,
                TriggerType = ContrapartidaDispatchBatchTriggerTypeEnum.Scheduled,
                RequestedBy = safeTriggeredBy,
                TriggeredAtUtc = DateTime.UtcNow,
                StartedAtUtc = DateTime.UtcNow,
                Status = ContrapartidaDispatchBatchStatusEnum.Processing,
                TotalItems = pendingItemIds.Count
            };
            await _context.ContrapartidaDispatchBatches.AddAsync(batch, ct);
            await _context.SaveChangesAsync(ct);

            await _context.ContrapartidaDispatchItems
                .Where(i => pendingItemIds.Contains(i.Id)
                    && EligibleStates.Contains(i.State)
                    && (!i.NextAttemptAtUtc.HasValue || i.NextAttemptAtUtc <= DateTime.UtcNow))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.State, ContrapartidaDispatchItemStateEnum.ReportingContrapartida)
                    .SetProperty(i => i.LastCorrelationId, operationToken)
                    .SetProperty(i => i.LastDispatchedBy, safeTriggeredBy)
                    .SetProperty(i => i.UpdatedAt, DateTimeOffset.UtcNow), ct);

            var dispatchItems = await _context.ContrapartidaDispatchItems
                .Where(i => pendingItemIds.Contains(i.Id)
                    && i.State == ContrapartidaDispatchItemStateEnum.ReportingContrapartida
                    && i.LastCorrelationId == operationToken)
                .ToListAsync(ct);

            if (dispatchItems.Count == 0)
            {
                batch.Status = ContrapartidaDispatchBatchStatusEnum.Cancelled;
                batch.SummaryMessage = "Sin items reclamados para este chunk (ya tomados por otro worker).";
                batch.FinishedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
                continue;
            }

            var transactionIds = dispatchItems.Select(i => i.AchTransactionId).ToArray();
            var transactions = await _context.AchTransactions
                .AsNoTracking()
                .Include(t => t.Addendas)
                .Where(t => transactionIds.Contains(t.Id))
                .OrderBy(t => t.Id)
                .ToListAsync(ct);

            string requestPayload = string.Empty;
            string responsePayload = string.Empty;
            ProcContrapartidasParsedResponse? parseResult = null;
            var startedAtUtc = DateTime.UtcNow;

            try
            {
                var resolution = await _procContrapartidasRequestMapper.ResolveAsync(cycle, transactions, DateTime.Now, ct);
                requestPayload = _procContrapartidasRequestMapper.BuildSoapBody(resolution.Contract);
                batch.MappingSetId = resolution.MappingSetId;
                batch.MappingVersion = resolution.MappingVersion;
                batch.MappingSnapshotHash = resolution.UsedFallback
                    ? "FALLBACK_TRANSITIONAL"
                    : resolution.MappingSnapshotHash;
                if (resolution.UsedFallback)
                {
                    _logger.LogWarning(
                        "Se ejecutó Proc_Contrapartidas con fallback transicional para ciclo {CycleId} cámara {ClearingHouseId}.",
                        cycle.Id,
                        cycle.ClearingHouseId);
                }

                responsePayload = await _soapClient.ProcContrapartidasAsync(requestPayload, ct);
                parseResult = _responseParser.Parse(responsePayload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error SOAP en Proc_Contrapartidas para ciclo {CycleId} cámara {ClearingHouseId}",
                    cycle.Id,
                    cycle.ClearingHouseId);

                parseResult = new ProcContrapartidasParsedResponse(
                    IsSuccess: false,
                    IsSoapFault: false,
                    IsRetryable: true,
                    IsFunctionalRejection: false,
                    ErrorCode: "SOAP_EXCEPTION",
                    ErrorMessage: ex.Message,
                    RawResponse: ex.ToString(),
                    ResponseCode: "SOAP_EXCEPTION",
                    ItemResults: new Dictionary<int, ProcContrapartidasParsedItemResponse>());
                responsePayload = ex.ToString();
            }

            var finishedAtUtc = DateTime.UtcNow;
            batch.RequestPayloadXml = requestPayload;
            batch.ResponsePayloadXml = responsePayload;

            foreach (var item in dispatchItems)
            {
                var txId = item.AchTransactionId;
                var hasItemResult = parseResult.ItemResults.TryGetValue(txId, out var txResult);

                var isSuccess = hasItemResult ? txResult!.IsSuccess : parseResult.IsSuccess;
                var itemCode = hasItemResult ? txResult!.ResponseCode : parseResult.ErrorCode;
                var itemMessage = hasItemResult ? txResult!.Message : parseResult.ErrorMessage;

                var attempt = new ContrapartidaDispatchAttempt
                {
                    DispatchItemId = item.Id,
                    DispatchBatchId = batch.Id,
                    AttemptNumber = item.AttemptCount + 1,
                    StartedAtUtc = startedAtUtc,
                    FinishedAtUtc = finishedAtUtc,
                    Result = isSuccess
                        ? ContrapartidaDispatchAttemptResultEnum.Success
                        : (hasItemResult && txResult is not null && !txResult.IsSuccess && parseResult.ItemResults.Count > 0 && parseResult.ItemResults.Values.Any(x => x.IsSuccess))
                            ? ContrapartidaDispatchAttemptResultEnum.Partial
                            : ContrapartidaDispatchAttemptResultEnum.Failed,
                    CorrelationId = $"{batch.Id:N}-{item.Id}",
                    TriggeredBy = safeTriggeredBy,
                    RetryEligible = !isSuccess && (hasItemResult ? txResult!.IsRetryable : parseResult.IsRetryable),
                    ExternalResponseCode = itemCode,
                    ExternalResponseMessage = itemMessage ?? string.Empty,
                    ErrorCode = isSuccess ? string.Empty : itemCode,
                    ErrorMessage = isSuccess ? string.Empty : (itemMessage ?? "Error de negocio en contrapartidas"),
                    RequestPayloadXml = requestPayload,
                    ResponsePayloadXml = responsePayload
                };

                _context.ContrapartidaDispatchAttempts.Add(attempt);

                item.AttemptCount += 1;
                item.LastAttemptAtUtc = finishedAtUtc;
                item.LastCorrelationId = attempt.CorrelationId;
                item.LastDispatchedBy = safeTriggeredBy;
                item.LastResponseCode = itemCode;
                item.LastErrorCode = isSuccess ? string.Empty : itemCode;
                item.LastErrorMessage = isSuccess ? string.Empty : (itemMessage ?? string.Empty);

                if (isSuccess)
                {
                    item.State = ContrapartidaDispatchItemStateEnum.ReportedToContrapartida;
                    item.LastSuccessAtUtc = finishedAtUtc;
                    item.NextAttemptAtUtc = null;
                    succeeded++;
                }
                else
                {
                    var retryable = hasItemResult ? txResult!.IsRetryable : parseResult.IsRetryable;
                    var canRetry = retryable && item.AttemptCount < MaxAttempts;
                    item.State = canRetry
                        ? ContrapartidaDispatchItemStateEnum.RetryPending
                        : ContrapartidaDispatchItemStateEnum.ContrapartidaReportFailed;
                    item.NextAttemptAtUtc = canRetry ? CalculateNextAttemptAtUtc(item.AttemptCount) : null;
                    failed++;
                    if (hasItemResult && parseResult.ItemResults.Count > 0 && parseResult.ItemResults.Values.Any(x => x.IsSuccess))
                    {
                        partial++;
                        chunkPartialCount++;
                    }
                }

                processed++;
            }

            batch.TotalSucceeded = dispatchItems.Count(x => x.State == ContrapartidaDispatchItemStateEnum.ReportedToContrapartida);
            batch.TotalFailed = dispatchItems.Count(x => x.State != ContrapartidaDispatchItemStateEnum.ReportedToContrapartida);
            batch.TotalPartial = chunkPartialCount;
            batch.FinishedAtUtc = finishedAtUtc;
            batch.Status = batch.TotalFailed == 0
                ? ContrapartidaDispatchBatchStatusEnum.Completed
                : batch.TotalSucceeded > 0
                    ? ContrapartidaDispatchBatchStatusEnum.CompletedWithErrors
                    : ContrapartidaDispatchBatchStatusEnum.Failed;
            batch.SummaryMessage = $"Chunk={chunks} Tx={dispatchItems.Count} Success={batch.TotalSucceeded} Failed={batch.TotalFailed} Code={parseResult.ResponseCode}";

            await _context.SaveChangesAsync(ct);
        }

        var summary = $"Ciclo {cycleId} cámara {clearingHouseId}: Processed={processed}, Success={succeeded}, Failed={failed}, Partial={partial}, Chunks={chunks}";
        return new ContrapartidaCycleDispatchResult(cycleId, clearingHouseId, processed, succeeded, failed, partial, chunks, summary);
    }

    public async Task<ContrapartidaBatchRetryResult> RetryBatchAsync(
        ContrapartidaDispatchRetryRequest request,
        CancellationToken ct = default)
    {
        var sourceBatch = await _context.ContrapartidaDispatchBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.SourceBatchId, ct)
            ?? throw new KeyNotFoundException($"No existe batch de contrapartida {request.SourceBatchId}.");

        if (sourceBatch.Status == ContrapartidaDispatchBatchStatusEnum.Processing)
        {
            throw new InvalidOperationException("No se puede reintentar un batch que está en procesamiento.");
        }

        var safeTriggeredBy = string.IsNullOrWhiteSpace(request.TriggeredBy) ? "manual:unknown" : request.TriggeredBy.Trim();
        var chunkSize = Math.Clamp(request.ChunkSize, 10, 2000);
        var retryJobId = $"manual-retry:{request.SourceBatchId:N}";

        var conflictingRetryInProgress = await _context.ContrapartidaDispatchBatches
            .AsNoTracking()
            .AnyAsync(x => x.JobId == retryJobId && x.Status == ContrapartidaDispatchBatchStatusEnum.Processing, ct);
        if (conflictingRetryInProgress)
        {
            throw new InvalidOperationException("Ya existe un reintento manual en ejecución para este batch origen.");
        }

        var cycle = await _context.AchCycles
            .AsNoTracking()
            .Include(c => c.ClearingHouseCycleConfig)
            .FirstOrDefaultAsync(x => x.Id == sourceBatch.AchCycleId && x.ClearingHouseId == sourceBatch.ClearingHouseId, ct)
            ?? throw new InvalidOperationException($"No existe ciclo {sourceBatch.AchCycleId} para cámara {sourceBatch.ClearingHouseId}.");
        ValidateCycleOperationalWindow(cycle, DateTime.Now);

        var sourceItemIds = await _context.ContrapartidaDispatchAttempts
            .AsNoTracking()
            .Where(a => a.DispatchBatchId == sourceBatch.Id)
            .Select(a => a.DispatchItemId)
            .Distinct()
            .ToListAsync(ct);

        if (sourceItemIds.Count == 0)
        {
            throw new InvalidOperationException("El batch origen no contiene items para reintentar.");
        }

        var items = await _context.ContrapartidaDispatchItems
            .AsNoTracking()
            .Where(i => sourceItemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.State, i.AttemptCount })
            .ToListAsync(ct);

        var lastAttempts = await _context.ContrapartidaDispatchAttempts
            .AsNoTracking()
            .Where(a => sourceItemIds.Contains(a.DispatchItemId))
            .GroupBy(a => a.DispatchItemId)
            .Select(g => g.OrderByDescending(x => x.AttemptNumber).Select(x => new { x.DispatchItemId, x.RetryEligible }).First())
            .ToListAsync(ct);

        var lastAttemptByItemId = lastAttempts.ToDictionary(x => x.DispatchItemId, x => x.RetryEligible);

        var selectedItemIds = items
            .Where(i =>
            {
                if (i.AttemptCount >= MaxAttempts)
                {
                    return false;
                }

                if (request.Scope == ContrapartidaDispatchRetryScope.Full)
                {
                    if (!request.AllowReplaySucceeded && i.State == ContrapartidaDispatchItemStateEnum.ReportedToContrapartida)
                    {
                        return false;
                    }
                    return i.State != ContrapartidaDispatchItemStateEnum.ReportingContrapartida
                        && i.State != ContrapartidaDispatchItemStateEnum.Retrying;
                }

                if (i.State == ContrapartidaDispatchItemStateEnum.ReportedToContrapartida)
                {
                    return false;
                }

                return lastAttemptByItemId.TryGetValue(i.Id, out var retryEligible) && retryEligible;
            })
            .Select(i => i.Id)
            .ToList();

        if (selectedItemIds.Count == 0)
        {
            throw new InvalidOperationException("No existen items elegibles para reintento según alcance solicitado.");
        }

        var processingBatch = new ContrapartidaDispatchBatch
        {
            AchCycleId = sourceBatch.AchCycleId,
            ClearingHouseId = sourceBatch.ClearingHouseId,
            TriggerType = ContrapartidaDispatchBatchTriggerTypeEnum.ManualRetry,
            RequestedBy = safeTriggeredBy,
            JobId = retryJobId,
            TriggeredAtUtc = DateTime.UtcNow,
            StartedAtUtc = DateTime.UtcNow,
            Status = ContrapartidaDispatchBatchStatusEnum.Processing,
            TotalItems = selectedItemIds.Count,
            SummaryMessage = $"Retry manual scope={request.Scope}"
        };
        await _context.ContrapartidaDispatchBatches.AddAsync(processingBatch, ct);
        await _context.SaveChangesAsync(ct);

        var totalProcessed = 0;
        var totalSucceeded = 0;
        var totalFailed = 0;
        var totalPartial = 0;
        var operationToken = $"manual-retry:{processingBatch.Id:N}";

        while (selectedItemIds.Count > 0)
        {
            var chunkIds = selectedItemIds.Take(chunkSize).ToList();
            selectedItemIds.RemoveRange(0, chunkIds.Count);

            await _context.ContrapartidaDispatchItems
                .Where(i => chunkIds.Contains(i.Id)
                    && i.State != ContrapartidaDispatchItemStateEnum.ReportingContrapartida
                    && i.State != ContrapartidaDispatchItemStateEnum.Retrying
                    && i.AttemptCount < MaxAttempts)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.State, ContrapartidaDispatchItemStateEnum.Retrying)
                    .SetProperty(i => i.LastCorrelationId, operationToken)
                    .SetProperty(i => i.LastDispatchedBy, safeTriggeredBy)
                    .SetProperty(i => i.UpdatedAt, DateTimeOffset.UtcNow), ct);

            var claimed = await _context.ContrapartidaDispatchItems
                .Where(i => chunkIds.Contains(i.Id)
                    && i.State == ContrapartidaDispatchItemStateEnum.Retrying
                    && i.LastCorrelationId == operationToken)
                .ToListAsync(ct);

            if (claimed.Count == 0)
            {
                continue;
            }

            var txIds = claimed.Select(x => x.AchTransactionId).ToArray();
            var txs = await _context.AchTransactions
                .AsNoTracking()
                .Include(t => t.Addendas)
                .Where(t => txIds.Contains(t.Id))
                .OrderBy(t => t.Id)
                .ToListAsync(ct);

            var startedAtUtc = DateTime.UtcNow;
            string requestPayload = string.Empty;
            string responsePayload = string.Empty;
            ProcContrapartidasParsedResponse parseResult;
            try
            {
                var resolution = await _procContrapartidasRequestMapper.ResolveAsync(cycle, txs, DateTime.Now, ct);
                requestPayload = _procContrapartidasRequestMapper.BuildSoapBody(resolution.Contract);
                processingBatch.MappingSetId = resolution.MappingSetId;
                processingBatch.MappingVersion = resolution.MappingVersion;
                processingBatch.MappingSnapshotHash = resolution.UsedFallback
                    ? "FALLBACK_TRANSITIONAL"
                    : resolution.MappingSnapshotHash;
                if (resolution.UsedFallback)
                {
                    _logger.LogWarning(
                        "Reintento manual ejecutó Proc_Contrapartidas con fallback transicional para ciclo {CycleId} cámara {ClearingHouseId}.",
                        cycle.Id,
                        cycle.ClearingHouseId);
                }
                responsePayload = await _soapClient.ProcContrapartidasAsync(requestPayload, ct);
                parseResult = _responseParser.Parse(responsePayload);
            }
            catch (Exception ex)
            {
                parseResult = new ProcContrapartidasParsedResponse(
                    IsSuccess: false,
                    IsSoapFault: false,
                    IsRetryable: true,
                    IsFunctionalRejection: false,
                    ErrorCode: "SOAP_EXCEPTION",
                    ErrorMessage: ex.Message,
                    RawResponse: ex.ToString(),
                    ResponseCode: "SOAP_EXCEPTION",
                    ItemResults: new Dictionary<int, ProcContrapartidasParsedItemResponse>());
                responsePayload = ex.ToString();
            }

            var finishedAtUtc = DateTime.UtcNow;
            foreach (var item in claimed)
            {
                var hasItemResult = parseResult.ItemResults.TryGetValue(item.AchTransactionId, out var txResult);
                var isSuccess = hasItemResult ? txResult!.IsSuccess : parseResult.IsSuccess;
                var code = hasItemResult ? txResult!.ResponseCode : parseResult.ErrorCode;
                var message = hasItemResult ? txResult!.Message : parseResult.ErrorMessage;
                var retryable = !isSuccess && (hasItemResult ? txResult!.IsRetryable : parseResult.IsRetryable);
                var canRetry = retryable && item.AttemptCount + 1 < MaxAttempts;

                _context.ContrapartidaDispatchAttempts.Add(new ContrapartidaDispatchAttempt
                {
                    DispatchItemId = item.Id,
                    DispatchBatchId = processingBatch.Id,
                    AttemptNumber = item.AttemptCount + 1,
                    StartedAtUtc = startedAtUtc,
                    FinishedAtUtc = finishedAtUtc,
                    Result = isSuccess
                        ? ContrapartidaDispatchAttemptResultEnum.Success
                        : (hasItemResult && parseResult.ItemResults.Values.Any(x => x.IsSuccess)
                            ? ContrapartidaDispatchAttemptResultEnum.Partial
                            : ContrapartidaDispatchAttemptResultEnum.Failed),
                    CorrelationId = $"{processingBatch.Id:N}-{item.Id}-{item.AttemptCount + 1}",
                    TriggeredBy = safeTriggeredBy,
                    RetryEligible = canRetry,
                    ExternalResponseCode = code,
                    ExternalResponseMessage = message ?? string.Empty,
                    ErrorCode = isSuccess ? string.Empty : code,
                    ErrorMessage = isSuccess ? string.Empty : (message ?? "Error de negocio en contrapartidas"),
                    RequestPayloadXml = requestPayload,
                    ResponsePayloadXml = responsePayload
                });

                item.AttemptCount += 1;
                item.LastAttemptAtUtc = finishedAtUtc;
                item.LastDispatchedBy = safeTriggeredBy;
                item.LastResponseCode = code;
                item.LastErrorCode = isSuccess ? string.Empty : code;
                item.LastErrorMessage = isSuccess ? string.Empty : (message ?? string.Empty);
                item.LastCorrelationId = $"{processingBatch.Id:N}-{item.Id}-{item.AttemptCount}";

                if (isSuccess)
                {
                    item.State = ContrapartidaDispatchItemStateEnum.ReportedToContrapartida;
                    item.LastSuccessAtUtc = finishedAtUtc;
                    item.NextAttemptAtUtc = null;
                    totalSucceeded++;
                }
                else
                {
                    item.State = canRetry
                        ? ContrapartidaDispatchItemStateEnum.RetryPending
                        : ContrapartidaDispatchItemStateEnum.ContrapartidaReportFailed;
                    item.NextAttemptAtUtc = canRetry ? CalculateNextAttemptAtUtc(item.AttemptCount) : null;
                    totalFailed++;
                    if (hasItemResult && parseResult.ItemResults.Values.Any(x => x.IsSuccess))
                    {
                        totalPartial++;
                    }
                }

                totalProcessed++;
            }

            processingBatch.RequestPayloadXml = requestPayload;
            processingBatch.ResponsePayloadXml = responsePayload;
            await _context.SaveChangesAsync(ct);
        }

        processingBatch.TotalSucceeded = totalSucceeded;
        processingBatch.TotalFailed = totalFailed;
        processingBatch.TotalPartial = totalPartial;
        processingBatch.FinishedAtUtc = DateTime.UtcNow;
        processingBatch.Status = totalFailed == 0
            ? ContrapartidaDispatchBatchStatusEnum.Completed
            : totalSucceeded > 0
                ? ContrapartidaDispatchBatchStatusEnum.CompletedWithErrors
                : ContrapartidaDispatchBatchStatusEnum.Failed;
        processingBatch.SummaryMessage = $"Retry source={request.SourceBatchId} scope={request.Scope} selected={processingBatch.TotalItems} processed={totalProcessed} success={totalSucceeded} failed={totalFailed}";

        await _context.SaveChangesAsync(ct);

        return new ContrapartidaBatchRetryResult(
            request.SourceBatchId,
            processingBatch.Id,
            sourceBatch.AchCycleId,
            sourceBatch.ClearingHouseId,
            processingBatch.TotalItems,
            totalProcessed,
            totalSucceeded,
            totalFailed,
            totalPartial,
            processingBatch.SummaryMessage);
    }

    private static void ValidateCycleOperationalWindow(AchCycle cycle, DateTime nowLocal)
    {
        if (cycle.ClearingHouseCycleConfig is not null)
        {
            if (!cycle.ClearingHouseCycleConfig.IsActive)
            {
                throw new InvalidOperationException($"Configuración de ciclo inactiva para {cycle.Id}.");
            }

            var processingDate = cycle.ProcessingDate.Date;
            var effectiveFrom = cycle.ClearingHouseCycleConfig.EffectiveFrom.Date;
            var effectiveTo = cycle.ClearingHouseCycleConfig.EffectiveTo?.Date;
            if (processingDate < effectiveFrom || (effectiveTo.HasValue && processingDate > effectiveTo.Value))
            {
                throw new InvalidOperationException($"Configuración de ciclo fuera de vigencia para {cycle.Id}.");
            }
        }

        var (windowStart, windowEnd) = BuildCycleWindow(cycle.ProcessingDate, cycle.StartTime, cycle.EndTime);
        if (nowLocal < windowStart || nowLocal > windowEnd)
        {
            throw new InvalidOperationException($"Ciclo {cycle.Id} fuera de ventana operativa: {windowStart:yyyy-MM-dd HH:mm} - {windowEnd:yyyy-MM-dd HH:mm}.");
        }
    }

    private static (DateTime Start, DateTime End) BuildCycleWindow(DateTime processingDate, TimeSpan startTime, TimeSpan endTime)
    {
        if (startTime <= endTime)
        {
            return (processingDate.Date + startTime, processingDate.Date + endTime);
        }

        return (processingDate.Date.AddDays(-1) + startTime, processingDate.Date + endTime);
    }

    private static DateTime CalculateNextAttemptAtUtc(int attemptCount)
    {
        var exponent = Math.Clamp(attemptCount, 1, 5);
        var delay = TimeSpan.FromMinutes(Math.Pow(2, exponent));
        var capped = delay > TimeSpan.FromMinutes(30) ? TimeSpan.FromMinutes(30) : delay;
        return DateTime.UtcNow.Add(capped);
    }

}
