using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class ContrapartidaDispatchJobService : IContrapartidaDispatchJobService
{
    private const int MaxAttempts = 5;
    private const string SoapMethodName = IntegrationGuaranteeConstants.ProcContrapartidas;
    private const string TechnicalStatusSucceeded = "Succeeded";
    private const string TechnicalStatusFunctionalRejection = "FunctionalRejection";
    private const string TechnicalStatusSoapFault = "SoapFault";
    private const string TechnicalStatusRetryableFailure = "RetryableFailure";
    private const string TechnicalStatusParserError = "ParserError";
    private const string TechnicalStatusTechnicalException = "TechnicalException";
    private const string TechnicalStatusDryRun = "DryRun";
    private const string TechnicalStatusDisabled = "Disabled";
    private const string TechnicalStatusUnknownFailure = "UnknownFailure";
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
    private readonly ProcContrapartidasDispatchOptions _dispatchOptions;
    private readonly ITransactionIntegrationOperationResolver? _operationResolver;
    private readonly IIntegrationMappingReadinessService? _mappingReadinessService;
    private readonly ISoapIntegrationSettingsService? _soapIntegrationSettingsService;
    private readonly IIntegrationResponseCatalogResolver? _responseCatalogResolver;
    private readonly TimeProvider _timeProvider;
    private readonly IOperationalCycleWindowResolver _windowResolver;

    public ContrapartidaDispatchJobService(
        AchDbContext context,
        IWscfaachSoapClient soapClient,
        IProcContrapartidasRequestMapper procContrapartidasRequestMapper,
        IProcContrapartidasResponseParser responseParser,
        ILogger<ContrapartidaDispatchJobService> logger,
        IOptions<ProcContrapartidasDispatchOptions>? dispatchOptions = null,
        ITransactionIntegrationOperationResolver? operationResolver = null,
        IIntegrationMappingReadinessService? mappingReadinessService = null,
        ISoapIntegrationSettingsService? soapIntegrationSettingsService = null,
        IIntegrationResponseCatalogResolver? responseCatalogResolver = null,
        TimeProvider? timeProvider = null,
        IOperationalCycleWindowResolver? windowResolver = null)
    {
        _context = context;
        _soapClient = soapClient;
        _procContrapartidasRequestMapper = procContrapartidasRequestMapper;
        _responseParser = responseParser;
        _logger = logger;
        _dispatchOptions = dispatchOptions?.Value ?? new ProcContrapartidasDispatchOptions();
        _operationResolver = operationResolver;
        _mappingReadinessService = mappingReadinessService;
        _soapIntegrationSettingsService = soapIntegrationSettingsService;
        _responseCatalogResolver = responseCatalogResolver;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _windowResolver = windowResolver ?? new OperationalCycleWindowResolver();
    }

    public async Task<ContrapartidaCycleDispatchResult> ProcessCycleAsync(
        string cycleId,
        int clearingHouseId,
        string triggeredBy,
        int chunkSize,
        CancellationToken ct = default)
        => await ProcessPendingAsync(cycleId, clearingHouseId, triggeredBy, chunkSize, null, ct);

    public async Task<ContrapartidaCycleDispatchResult> ProcessTransactionAsync(
        string cycleId,
        int clearingHouseId,
        int transactionId,
        string triggeredBy,
        CancellationToken ct = default)
    {
        if (transactionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transactionId), "El identificador de transaccion debe ser positivo.");
        }

        return await ProcessPendingAsync(cycleId, clearingHouseId, triggeredBy, 1, transactionId, ct);
    }

    public async Task<ContrapartidaCycleDispatchResult> ProcessTransactionAsync(
        int transactionId,
        string triggeredBy,
        CancellationToken ct = default)
    {
        if (transactionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transactionId), "El identificador de transaccion debe ser positivo.");
        }

        var target = await _context.ContrapartidaDispatchItems
            .AsNoTracking()
            .Where(x => x.AchTransactionId == transactionId)
            .Select(x => new
            {
                x.AchCycleId,
                x.ClearingHouseId,
                x.State,
                HasSuccessfulAttempt = x.Attempts.Any(a =>
                    a.BusinessStatus == IntegrationResponseBusinessStatus.Success
                    || a.IsSuccessful)
            })
            .SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("No existe una cola de Proc_Contrapartidas para la transacción solicitada.");

        if (target.State == ContrapartidaDispatchItemStateEnum.ReportedToContrapartida
            || target.HasSuccessfulAttempt)
        {
            throw new InvalidOperationException(
                "CONTRAPARTIDA_ALREADY_SUCCEEDED: la transacción ya tiene un resultado funcional exitoso.");
        }

        return await ProcessPendingAsync(
            target.AchCycleId,
            target.ClearingHouseId,
            triggeredBy,
            1,
            transactionId,
            ct);
    }

    private async Task<ContrapartidaCycleDispatchResult> ProcessPendingAsync(
        string cycleId,
        int clearingHouseId,
        string triggeredBy,
        int chunkSize,
        int? transactionId,
        CancellationToken ct)
    {
        chunkSize = Math.Clamp(chunkSize, 10, 2000);
        var safeTriggeredBy = string.IsNullOrWhiteSpace(triggeredBy) ? "quartz:contrapartida" : triggeredBy.Trim();
        var nowUtcOffset = _timeProvider.GetUtcNow();
        var nowUtc = nowUtcOffset.UtcDateTime;

        var cycle = await _context.AchCycles
            .AsNoTracking()
            .Include(c => c.ClearingHouse)
                .ThenInclude(clearingHouse => clearingHouse!.ClearingHouseConfig)
            .Include(c => c.ClearingHouseCycleConfig)
            .FirstOrDefaultAsync(c => c.Id == cycleId && c.ClearingHouseId == clearingHouseId, ct)
            ?? throw new InvalidOperationException($"No existe ciclo {cycleId} para cámara {clearingHouseId}.");

        var nowLocal = ValidateCycleOperationalWindow(cycle, nowUtcOffset).LocalNow;

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
                .Where(i => i.AchTransaction.ClassificationStatus == AchTransactionClassificationStatus.Determined
                    && i.AchTransaction.MonetaryIntegrationRoute == AchMonetaryIntegrationRoute.ProcContrapartidas
                    && i.AchTransaction.Direction == AchTransactionDirection.Outgoing
                    && i.AchTransaction.Origin == AchTransactionOrigin.Cfa
                    && i.AchTransaction.Type == TransactionTypeEnum.Debit)
                .Where(i => !transactionId.HasValue || i.AchTransactionId == transactionId.Value)
                .Where(i => EligibleStates.Contains(i.State))
                .Where(i => !i.NextAttemptAtUtc.HasValue || i.NextAttemptAtUtc <= nowUtc)
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
                TriggeredAtUtc = nowUtc,
                StartedAtUtc = nowUtc,
                Status = ContrapartidaDispatchBatchStatusEnum.Processing,
                TotalItems = pendingItemIds.Count
            };
            await _context.ContrapartidaDispatchBatches.AddAsync(batch, ct);
            await _context.SaveChangesAsync(ct);

            await _context.ContrapartidaDispatchItems
                .Where(i => pendingItemIds.Contains(i.Id)
                    && EligibleStates.Contains(i.State)
                    && (!i.NextAttemptAtUtc.HasValue || i.NextAttemptAtUtc <= nowUtc))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.State, ContrapartidaDispatchItemStateEnum.ReportingContrapartida)
                    .SetProperty(i => i.LastCorrelationId, operationToken)
                    .SetProperty(i => i.LastDispatchedBy, safeTriggeredBy)
                    .SetProperty(i => i.UpdatedAt, nowUtcOffset), ct);

            var dispatchItems = await _context.ContrapartidaDispatchItems
                .Where(i => pendingItemIds.Contains(i.Id)
                    && i.State == ContrapartidaDispatchItemStateEnum.ReportingContrapartida
                    && i.LastCorrelationId == operationToken)
                .ToListAsync(ct);

            if (dispatchItems.Count == 0)
            {
                batch.Status = ContrapartidaDispatchBatchStatusEnum.Cancelled;
                batch.SummaryMessage = "Sin items reclamados para este chunk (ya tomados por otro worker).";
                batch.FinishedAtUtc = nowUtc;
                await _context.SaveChangesAsync(ct);
                continue;
            }

            var transactionIds = dispatchItems.Select(i => i.AchTransactionId).ToArray();
            var transactions = await _context.AchTransactions
                .Include(t => t.Addendas)
                .Where(t => transactionIds.Contains(t.Id))
                .OrderBy(t => t.Id)
                .ToListAsync(ct);
            var transactionById = transactions.ToDictionary(x => x.Id);
            var queueItems = await _context.IncomingNachaDispatchQueue
                .AsNoTracking()
                .Where(q => q.AchCycleId == cycle.Id && q.ClearingHouseId == cycle.ClearingHouseId && transactionIds.Contains(q.AchTransactionId))
                .ToListAsync(ct);
            var queueByTransactionId = queueItems
                .GroupBy(q => q.AchTransactionId)
                .ToDictionary(g => g.Key, g => g.First());

            string requestPayload = string.Empty;
            string responsePayload = string.Empty;
            string dispatchEndpoint = string.Empty;
            string executionMode = _dispatchOptions.NormalizedMode;
            string technicalException = string.Empty;
            ProcContrapartidasParsedResponse? parseResult = null;
            var startedAtUtc = nowUtc;

            try
            {
                await EnsureContrapartidasReadinessAsync(transactions, ct);
                var resolution = await _procContrapartidasRequestMapper.ResolveAsync(cycle, transactions, nowLocal, ct);
                EnsureNoFallbackResolution(resolution);
                requestPayload = _procContrapartidasRequestMapper.BuildSoapBody(resolution.Contract);
                var runtime = await ResolveProcContrapartidasRuntimeAsync(ct);
                dispatchEndpoint = runtime.Endpoint;
                executionMode = runtime.OperatingMode;
                batch.MappingSetId = resolution.MappingSetId;
                batch.MappingVersion = resolution.MappingVersion;
                batch.MappingSnapshotHash = resolution.MappingSnapshotHash;

                var dispatchResult = await DispatchProcContrapartidasAsync(
                    requestPayload,
                    cycle.Id,
                    cycle.ClearingHouseId,
                    executionMode,
                    ct);
                responsePayload = dispatchResult.ResponsePayload;
                parseResult = dispatchResult.ParseResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error SOAP en Proc_Contrapartidas para ciclo {CycleId} cámara {ClearingHouseId}",
                    cycle.Id,
                    cycle.ClearingHouseId);

                technicalException = BuildTechnicalException(ex);
                parseResult = new ProcContrapartidasParsedResponse(
                    IsSuccess: false,
                    IsSoapFault: false,
                    IsRetryable: true,
                    IsFunctionalRejection: false,
                    ErrorCode: "SOAP_EXCEPTION",
                    ErrorMessage: ex.Message,
                    RawResponse: technicalException,
                    ResponseCode: "SOAP_EXCEPTION",
                    ItemResults: new Dictionary<int, ProcContrapartidasParsedItemResponse>());
                responsePayload = technicalException;
            }

            var finishedAtUtc = nowUtc;
            var durationMs = CalculateDurationMs(startedAtUtc, finishedAtUtc);
            batch.RequestPayloadXml = requestPayload;
            batch.ResponsePayloadXml = responsePayload;

            foreach (var item in dispatchItems)
            {
                var txId = item.AchTransactionId;
                var hasItemResult = parseResult.ItemResults.TryGetValue(txId, out var txResult);

                var itemCode = hasItemResult ? txResult!.ResponseCode : parseResult.ResponseCode;
                var transportStatus = ResolveTransportStatus(parseResult, technicalException, executionMode);
                var catalogResult = transportStatus == IntegrationTransportStatus.Succeeded
                    ? await ResolveCoreResponseAsync(itemCode, finishedAtUtc, ct)
                    : null;
                var businessStatus = catalogResult?.BusinessStatus ?? IntegrationResponseBusinessStatus.Unknown;
                var isSuccess = transportStatus == IntegrationTransportStatus.Succeeded
                    && catalogResult is { IsKnownCode: true, BusinessStatus: IntegrationResponseBusinessStatus.Success };
                var retryAllowed = catalogResult is null
                    ? (hasItemResult ? txResult!.IsRetryable : parseResult.IsRetryable)
                    : catalogResult.RetryAllowed;
                var itemMessage = catalogResult?.Description
                    ?? (hasItemResult ? txResult!.Message : parseResult.ErrorMessage);
                var soapAudit = BuildAttemptSoapAuditFields(
                    parseResult,
                    hasItemResult ? txResult : null,
                    isSuccess,
                    itemCode,
                    itemMessage,
                    dispatchEndpoint,
                    durationMs,
                    technicalException,
                    executionMode);

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
                    RetryEligible = !isSuccess && retryAllowed,
                    ExternalResponseCode = itemCode,
                    ExternalResponseMessage = itemMessage ?? string.Empty,
                    ErrorCode = isSuccess ? string.Empty : itemCode,
                    ErrorMessage = isSuccess ? string.Empty : (itemMessage ?? "Error de negocio en contrapartidas"),
                    RequestPayloadXml = requestPayload,
                    ResponsePayloadXml = responsePayload,
                    SoapMethodName = soapAudit.SoapMethodName,
                    SoapEndpoint = soapAudit.SoapEndpoint,
                    ExecutionMode = soapAudit.ExecutionMode,
                    DurationMs = soapAudit.DurationMs,
                    SoapResponseCode = soapAudit.SoapResponseCode,
                    SoapResponseDescription = NormalizeAuditValue(itemMessage, 4000),
                    SoapTechnicalStatus = transportStatus == IntegrationTransportStatus.Succeeded
                        ? TechnicalStatusSucceeded
                        : soapAudit.SoapTechnicalStatus,
                    ResponseCatalogId = catalogResult?.CatalogId,
                    TransportStatus = transportStatus,
                    BusinessStatus = businessStatus,
                    RetryAllowed = retryAllowed,
                    RequiresManualReview = catalogResult?.RequiresManualReview ?? false,
                    ProcessedAtUtc = finishedAtUtc,
                    IsSuccessful = isSuccess,
                    IsFunctionalRejection = businessStatus == IntegrationResponseBusinessStatus.Rejected,
                    IsTechnicalFailure = transportStatus is IntegrationTransportStatus.Failed or IntegrationTransportStatus.TimedOut,
                    TechnicalException = soapAudit.TechnicalException
                };

                _context.ContrapartidaDispatchAttempts.Add(attempt);
                if (queueByTransactionId.TryGetValue(txId, out var queue))
                {
                    _context.IncomingNachaIntegrationExecution.Add(new IncomingNachaIntegrationExecution
                    {
                        DispatchQueueId = queue.Id,
                        MethodName = IntegrationGuaranteeConstants.ProcContrapartidas,
                        SoapMethodName = soapAudit.SoapMethodName,
                        SoapEndpoint = soapAudit.SoapEndpoint,
                        ExecutionMode = soapAudit.ExecutionMode,
                        MappingSetId = batch.MappingSetId,
                        MappingVersion = batch.MappingVersion,
                        MappingSnapshotHash = batch.MappingSnapshotHash,
                        RequestHash = Hash(requestPayload),
                        ResponseHash = Hash(responsePayload),
                        RequestPayloadXml = requestPayload,
                        ResponsePayloadXml = responsePayload,
                        SoapResponseCode = soapAudit.SoapResponseCode,
                        SoapResponseDescription = NormalizeAuditValue(itemMessage, 4000),
                        SoapTechnicalStatus = transportStatus == IntegrationTransportStatus.Succeeded
                            ? TechnicalStatusSucceeded
                            : soapAudit.SoapTechnicalStatus,
                        ResponseCatalogId = catalogResult?.CatalogId,
                        TransportStatus = transportStatus,
                        BusinessStatus = businessStatus,
                        RetryAllowed = retryAllowed,
                        RequiresManualReview = catalogResult?.RequiresManualReview ?? false,
                        ProcessedAtUtc = finishedAtUtc,
                        IsSuccessful = isSuccess,
                        IsFunctionalRejection = businessStatus == IntegrationResponseBusinessStatus.Rejected,
                        IsTechnicalFailure = transportStatus is IntegrationTransportStatus.Failed or IntegrationTransportStatus.TimedOut,
                        TechnicalException = soapAudit.TechnicalException,
                        DurationMs = soapAudit.DurationMs,
                        ResponseCode = itemCode,
                        ResponseMessage = itemMessage ?? string.Empty,
                        IsSuccess = isSuccess,
                        IsRetryable = !isSuccess && retryAllowed,
                        StartedAtUtc = startedAtUtc,
                        FinishedAtUtc = finishedAtUtc,
                        CorrelationId = attempt.CorrelationId
                    });
                }

                item.AttemptCount += 1;
                item.LastAttemptAtUtc = finishedAtUtc;
                item.LastCorrelationId = attempt.CorrelationId;
                item.LastDispatchedBy = safeTriggeredBy;
                item.LastResponseCode = itemCode;
                item.LastErrorCode = isSuccess ? string.Empty : itemCode;
                item.LastErrorMessage = isSuccess ? string.Empty : (itemMessage ?? string.Empty);

                if (isSuccess)
                {
                    if (transactionById.TryGetValue(txId, out var transaction) && catalogResult is not null)
                    {
                        ApplySuccessfulTransactionState(transaction, catalogResult, finishedAtUtc);
                    }
                    item.State = ContrapartidaDispatchItemStateEnum.ReportedToContrapartida;
                    item.LastSuccessAtUtc = finishedAtUtc;
                    item.NextAttemptAtUtc = null;
                    succeeded++;
                }
                else
                {
                    var canRetry = retryAllowed && item.AttemptCount < MaxAttempts;
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
            .Include(c => c.ClearingHouse)
                .ThenInclude(clearingHouse => clearingHouse!.ClearingHouseConfig)
            .Include(c => c.ClearingHouseCycleConfig)
            .FirstOrDefaultAsync(x => x.Id == sourceBatch.AchCycleId && x.ClearingHouseId == sourceBatch.ClearingHouseId, ct)
            ?? throw new InvalidOperationException($"No existe ciclo {sourceBatch.AchCycleId} para cámara {sourceBatch.ClearingHouseId}.");
        var nowUtcOffset = _timeProvider.GetUtcNow();
        var nowUtc = nowUtcOffset.UtcDateTime;
        var nowLocal = ValidateCycleOperationalWindow(cycle, nowUtcOffset).LocalNow;

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
            .Where(i => i.AchTransaction.ClassificationStatus == AchTransactionClassificationStatus.Determined
                && i.AchTransaction.MonetaryIntegrationRoute == AchMonetaryIntegrationRoute.ProcContrapartidas
                && i.AchTransaction.Direction == AchTransactionDirection.Outgoing
                && i.AchTransaction.Origin == AchTransactionOrigin.Cfa
                && i.AchTransaction.Type == TransactionTypeEnum.Debit)
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
            TriggeredAtUtc = nowUtc,
            StartedAtUtc = nowUtc,
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
                    .SetProperty(i => i.UpdatedAt, nowUtcOffset), ct);

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
                .Include(t => t.Addendas)
                .Where(t => txIds.Contains(t.Id))
                .OrderBy(t => t.Id)
                .ToListAsync(ct);
            var txById = txs.ToDictionary(x => x.Id);

            var startedAtUtc = nowUtc;
            string requestPayload = string.Empty;
            string responsePayload = string.Empty;
            string dispatchEndpoint = string.Empty;
            string executionMode = _dispatchOptions.NormalizedMode;
            string technicalException = string.Empty;
            ProcContrapartidasParsedResponse parseResult;
            try
            {
                await EnsureContrapartidasReadinessAsync(txs, ct);
                var resolution = await _procContrapartidasRequestMapper.ResolveAsync(cycle, txs, nowLocal, ct);
                EnsureNoFallbackResolution(resolution);
                requestPayload = _procContrapartidasRequestMapper.BuildSoapBody(resolution.Contract);
                var runtime = await ResolveProcContrapartidasRuntimeAsync(ct);
                dispatchEndpoint = runtime.Endpoint;
                executionMode = runtime.OperatingMode;
                processingBatch.MappingSetId = resolution.MappingSetId;
                processingBatch.MappingVersion = resolution.MappingVersion;
                processingBatch.MappingSnapshotHash = resolution.MappingSnapshotHash;
                var dispatchResult = await DispatchProcContrapartidasAsync(
                    requestPayload,
                    cycle.Id,
                    cycle.ClearingHouseId,
                    executionMode,
                    ct);
                responsePayload = dispatchResult.ResponsePayload;
                parseResult = dispatchResult.ParseResult;
            }
            catch (Exception ex)
            {
                technicalException = BuildTechnicalException(ex);
                parseResult = new ProcContrapartidasParsedResponse(
                    IsSuccess: false,
                    IsSoapFault: false,
                    IsRetryable: true,
                    IsFunctionalRejection: false,
                    ErrorCode: "SOAP_EXCEPTION",
                    ErrorMessage: ex.Message,
                    RawResponse: technicalException,
                    ResponseCode: "SOAP_EXCEPTION",
                    ItemResults: new Dictionary<int, ProcContrapartidasParsedItemResponse>());
                responsePayload = technicalException;
            }

            var finishedAtUtc = nowUtc;
            var durationMs = CalculateDurationMs(startedAtUtc, finishedAtUtc);
            foreach (var item in claimed)
            {
                var hasItemResult = parseResult.ItemResults.TryGetValue(item.AchTransactionId, out var txResult);
                var code = hasItemResult ? txResult!.ResponseCode : parseResult.ResponseCode;
                var transportStatus = ResolveTransportStatus(parseResult, technicalException, executionMode);
                var catalogResult = transportStatus == IntegrationTransportStatus.Succeeded
                    ? await ResolveCoreResponseAsync(code, finishedAtUtc, ct)
                    : null;
                var businessStatus = catalogResult?.BusinessStatus ?? IntegrationResponseBusinessStatus.Unknown;
                var isSuccess = transportStatus == IntegrationTransportStatus.Succeeded
                    && catalogResult is { IsKnownCode: true, BusinessStatus: IntegrationResponseBusinessStatus.Success };
                var retryAllowed = catalogResult is null
                    ? (hasItemResult ? txResult!.IsRetryable : parseResult.IsRetryable)
                    : catalogResult.RetryAllowed;
                var canRetry = !isSuccess && retryAllowed && item.AttemptCount + 1 < MaxAttempts;
                var message = catalogResult?.Description
                    ?? (hasItemResult ? txResult!.Message : parseResult.ErrorMessage);
                var soapAudit = BuildAttemptSoapAuditFields(
                    parseResult,
                    hasItemResult ? txResult : null,
                    isSuccess,
                    code,
                    message,
                    dispatchEndpoint,
                    durationMs,
                    technicalException,
                    executionMode);

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
                    ResponsePayloadXml = responsePayload,
                    SoapMethodName = soapAudit.SoapMethodName,
                    SoapEndpoint = soapAudit.SoapEndpoint,
                    ExecutionMode = soapAudit.ExecutionMode,
                    DurationMs = soapAudit.DurationMs,
                    SoapResponseCode = soapAudit.SoapResponseCode,
                    SoapResponseDescription = NormalizeAuditValue(message, 4000),
                    SoapTechnicalStatus = transportStatus == IntegrationTransportStatus.Succeeded
                        ? TechnicalStatusSucceeded
                        : soapAudit.SoapTechnicalStatus,
                    ResponseCatalogId = catalogResult?.CatalogId,
                    TransportStatus = transportStatus,
                    BusinessStatus = businessStatus,
                    RetryAllowed = retryAllowed,
                    RequiresManualReview = catalogResult?.RequiresManualReview ?? false,
                    ProcessedAtUtc = finishedAtUtc,
                    IsSuccessful = isSuccess,
                    IsFunctionalRejection = businessStatus == IntegrationResponseBusinessStatus.Rejected,
                    IsTechnicalFailure = transportStatus is IntegrationTransportStatus.Failed or IntegrationTransportStatus.TimedOut,
                    TechnicalException = soapAudit.TechnicalException
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
                    if (txById.TryGetValue(item.AchTransactionId, out var transaction) && catalogResult is not null)
                    {
                        ApplySuccessfulTransactionState(transaction, catalogResult, finishedAtUtc);
                    }
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
        processingBatch.FinishedAtUtc = nowUtc;
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

    private async Task<ProcContrapartidasDispatchExecutionResult> DispatchProcContrapartidasAsync(
        string requestPayload,
        string cycleId,
        int clearingHouseId,
        string executionMode,
        CancellationToken ct)
    {
        if (IsLiveMode(executionMode))
        {
            _logger.LogInformation(
                "Proc_Contrapartidas live habilitado para ciclo {CycleId} camara {ClearingHouseId}.",
                cycleId,
                clearingHouseId);

            var responsePayload = await _soapClient.ProcContrapartidasAsync(requestPayload, ct);
            return new ProcContrapartidasDispatchExecutionResult(responsePayload, _responseParser.Parse(responsePayload));
        }

        var code = IsDisabledMode(executionMode)
            ? "PROC_DISABLED"
            : "PROC_DRY_RUN";
        var mode = IsDisabledMode(executionMode) ? "disabled" : "dry-run";
        var message = IsDisabledMode(executionMode)
            ? "Proc_Contrapartidas disabled: envelope generado, no transmitido."
            : "Proc_Contrapartidas dry-run: envelope generado, no transmitido.";

        _logger.LogInformation(
            "{Message} Mode={Mode} CycleId={CycleId} ClearingHouseId={ClearingHouseId}",
            message,
            mode,
            cycleId,
            clearingHouseId);

        var parseResult = new ProcContrapartidasParsedResponse(
            IsSuccess: false,
            IsSoapFault: false,
            IsRetryable: false,
            IsFunctionalRejection: false,
            ErrorCode: code,
            ErrorMessage: message,
            RawResponse: message,
            ResponseCode: code,
            ItemResults: new Dictionary<int, ProcContrapartidasParsedItemResponse>());

        return new ProcContrapartidasDispatchExecutionResult(message, parseResult);
    }

    private async Task EnsureContrapartidasReadinessAsync(IReadOnlyCollection<AchTransaction> transactions, CancellationToken ct)
    {
        if (_operationResolver is null || _mappingReadinessService is null)
        {
            return;
        }

        foreach (var transaction in transactions)
        {
            var operation = await _operationResolver.ResolveAsync(transaction, ct);
            if (!operation.IsSupported
                || operation.OperationKey != IntegrationGuaranteeConstants.ProcContrapartidas
                || operation.MappingPurpose != IntegrationGuaranteeConstants.MonetaryDebitRequest)
            {
                throw new InvalidOperationException(
                    $"INTEGRATION_OPERATION_MISMATCH: la transaccion {transaction.Id} no corresponde a Proc_Contrapartidas/MonetaryDebitRequest.");
            }

            var readiness = await _mappingReadinessService.EvaluateAsync(operation, ct);
            if (readiness.Status == "Failed")
            {
                throw new InvalidOperationException(
                    $"{readiness.Code}: no se puede construir envelope Proc_Contrapartidas para transaccion {transaction.Id}; faltan mappings requeridos.");
            }

            if (!readiness.CanBuildPayload)
            {
                throw new InvalidOperationException(
                    $"{readiness.Code}: readiness de Proc_Contrapartidas no permite construir payload para transaccion {transaction.Id}.");
            }

            if (readiness.UsesFallback)
            {
                _logger.LogWarning(
                    "Readiness parcial para Proc_Contrapartidas transactionId={TransactionId}: fallback transicional trazado antes de XML.",
                    transaction.Id);
            }
        }
    }

    private async Task<(string Endpoint, string OperatingMode)> ResolveProcContrapartidasRuntimeAsync(
        CancellationToken ct)
    {
        if (_soapIntegrationSettingsService is null)
        {
            return (string.Empty, _dispatchOptions.NormalizedMode);
        }

        var settings = await _soapIntegrationSettingsService.GetAsync(ct);
        var mapping = settings.WscfaachMappings
            .FirstOrDefault(x => string.Equals(x.MethodName, SoapMethodName, StringComparison.OrdinalIgnoreCase));
        var mode = mapping?.Enabled == false
            ? "Disabled"
            : NormalizeOperatingMode(mapping?.OperatingMode, _dispatchOptions.NormalizedMode);
        return (mapping?.Endpoint?.Trim() ?? string.Empty, mode);
    }

    private AttemptSoapAuditFields BuildAttemptSoapAuditFields(
        ProcContrapartidasParsedResponse parseResult,
        ProcContrapartidasParsedItemResponse? itemResult,
        bool isSuccess,
        string? responseCode,
        string? responseDescription,
        string soapEndpoint,
        long durationMs,
        string technicalException,
        string executionMode)
    {
        var normalizedCode = NormalizeAuditValue(
            string.IsNullOrWhiteSpace(responseCode) ? parseResult.ResponseCode : responseCode,
            80);
        var normalizedDescription = NormalizeAuditValue(
            string.IsNullOrWhiteSpace(responseDescription) ? parseResult.ErrorMessage : responseDescription,
            4000);
        var itemFunctionalRejection = itemResult is not null
            && !itemResult.IsSuccess
            && !itemResult.IsRetryable
            && !IsTechnicalAnomalyCode(itemResult.ResponseCode)
            && !parseResult.IsSoapFault;
        var functionalRejection = !isSuccess && (parseResult.IsFunctionalRejection || itemFunctionalRejection);
        var technicalFailure = !isSuccess
            && !functionalRejection
            && (!string.IsNullOrWhiteSpace(technicalException)
                || (!IsDryRunLikeMode(executionMode) && !IsDisabledMode(executionMode)));

        var technicalStatus = ResolveSoapTechnicalStatus(
            parseResult,
            isSuccess,
            functionalRejection,
            executionMode);

        return new AttemptSoapAuditFields(
            SoapMethodName,
            NormalizeAuditValue(soapEndpoint, 500),
            NormalizeAuditValue(executionMode, 20),
            durationMs,
            normalizedCode,
            normalizedDescription,
            technicalStatus,
            isSuccess,
            functionalRejection,
            technicalFailure,
            NormalizeAuditValue(technicalException, 4000));
    }

    private string ResolveSoapTechnicalStatus(
        ProcContrapartidasParsedResponse parseResult,
        bool isSuccess,
        bool isFunctionalRejection,
        string executionMode)
    {
        if (parseResult.ResponseCode.Equals("SOAP_EXCEPTION", StringComparison.OrdinalIgnoreCase))
        {
            return TechnicalStatusTechnicalException;
        }

        if (IsDisabledMode(executionMode))
        {
            return TechnicalStatusDisabled;
        }

        if (IsDryRunLikeMode(executionMode))
        {
            return TechnicalStatusDryRun;
        }

        if (isSuccess)
        {
            return TechnicalStatusSucceeded;
        }

        if (parseResult.IsSoapFault)
        {
            return TechnicalStatusSoapFault;
        }

        if (parseResult.ResponseCode.Equals("PARSER_ERROR", StringComparison.OrdinalIgnoreCase))
        {
            return TechnicalStatusParserError;
        }

        if (isFunctionalRejection)
        {
            return TechnicalStatusFunctionalRejection;
        }

        if (parseResult.IsRetryable)
        {
            return TechnicalStatusRetryableFailure;
        }

        return TechnicalStatusUnknownFailure;
    }

    private async Task<IntegrationResponseCatalogResult> ResolveCoreResponseAsync(
        string? responseCode,
        DateTime processedAtUtc,
        CancellationToken ct)
    {
        if (_responseCatalogResolver is not null)
        {
            return await _responseCatalogResolver.ResolveAsync(
                IntegrationResponseCategory.CoreSoapResponse,
                SoapMethodName,
                responseCode,
                processedAtUtc,
                ct);
        }

        return new IntegrationResponseCatalogResult(
            null,
            (responseCode ?? string.Empty).Trim().ToUpperInvariant(),
            "Código pendiente de parametrización",
            IntegrationResponseCategory.CoreSoapResponse,
            IntegrationResponseCategory.CoreSoapResponse,
            SoapMethodName,
            IntegrationResponseBusinessStatus.PendingCatalog,
            false,
            true,
            false,
            string.Empty,
            false);
    }

    private IntegrationTransportStatus ResolveTransportStatus(
        ProcContrapartidasParsedResponse parsed,
        string technicalException,
        string executionMode)
    {
        if (parsed.ResponseCode.Contains("TIMEOUT", StringComparison.OrdinalIgnoreCase)
            || technicalException.Contains("TIMEOUT", StringComparison.OrdinalIgnoreCase))
        {
            return IntegrationTransportStatus.TimedOut;
        }

        if (!string.IsNullOrWhiteSpace(technicalException)
            || parsed.IsSoapFault
            || IsTechnicalAnomalyCode(parsed.ResponseCode))
        {
            return IntegrationTransportStatus.Failed;
        }

        if (IsDisabledMode(executionMode) || IsDryRunLikeMode(executionMode))
        {
            return IntegrationTransportStatus.NotExecuted;
        }

        return IntegrationTransportStatus.Succeeded;
    }

    private static string NormalizeOperatingMode(string? mode, string fallback)
    {
        var candidate = string.IsNullOrWhiteSpace(mode) ? fallback : mode.Trim();
        if (string.Equals(candidate, "Live", StringComparison.OrdinalIgnoreCase))
        {
            return "Live";
        }

        if (string.Equals(candidate, "Disabled", StringComparison.OrdinalIgnoreCase))
        {
            return "Disabled";
        }

        return "DryRun";
    }

    private static bool IsLiveMode(string mode)
        => string.Equals(mode, "Live", StringComparison.OrdinalIgnoreCase);

    private static bool IsDisabledMode(string mode)
        => string.Equals(mode, "Disabled", StringComparison.OrdinalIgnoreCase);

    private static bool IsDryRunLikeMode(string mode)
        => !IsLiveMode(mode) && !IsDisabledMode(mode);

    private void ApplySuccessfulTransactionState(
        AchTransaction transaction,
        IntegrationResponseCatalogResult catalog,
        DateTime processedAtUtc)
    {
        transaction.ContrapartidasResponseCode = catalog.Code;

        if (!Enum.TryParse<AchTransferStateEnum>(catalog.TargetTransactionState, true, out var targetState)
            || transaction.State == targetState)
        {
            return;
        }

        if (transaction.State != AchTransferStateEnum.Pending
            || targetState != AchTransferStateEnum.AppliedTacitly)
        {
            throw new InvalidOperationException(
                $"CORE_RESPONSE_TARGET_STATE_INVALID: no se permite aplicar {catalog.TargetTransactionState} desde {transaction.State}.");
        }

        var previous = transaction.State;
        transaction.State = targetState;
        transaction.StateChangedAtUtc = processedAtUtc;
        _context.AchTransactionStateEvents.Add(new AchTransactionStateEvent
        {
            AchTransactionId = transaction.Id,
            FromState = previous,
            ToState = targetState,
            Source = AchStateEventSourceEnum.System,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                responseCatalogId = catalog.CatalogId,
                category = catalog.Category,
                method = catalog.Method,
                businessStatus = catalog.BusinessStatus.ToString()
            })
        });
    }

    private static void EnsureNoFallbackResolution(ProcContrapartidasRequestResolution resolution)
    {
        if (resolution.UsedFallback)
        {
            throw new InvalidOperationException(
                "REQUIRED_MAPPING_USES_FALLBACK: Proc_Contrapartidas no puede construir XML con fallback transicional. Configure mappings activos requeridos.");
        }
    }

    private static string Hash(string payload)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload ?? string.Empty));
        return Convert.ToHexString(bytes);
    }

    private static long CalculateDurationMs(DateTime startedAtUtc, DateTime finishedAtUtc)
        => Math.Max(0, (long)(finishedAtUtc - startedAtUtc).TotalMilliseconds);

    private static string BuildTechnicalException(Exception ex)
        => NormalizeAuditValue($"{ex.GetType().Name}: {ex.Message}", 4000);

    private static string NormalizeAuditValue(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static bool IsTechnicalAnomalyCode(string? code)
        => code is not null
           && (code.Equals("RE", StringComparison.OrdinalIgnoreCase)
               || code.Equals("0", StringComparison.OrdinalIgnoreCase)
               || code.Equals("SOAP_FAULT", StringComparison.OrdinalIgnoreCase)
               || code.Equals("PARSER_ERROR", StringComparison.OrdinalIgnoreCase)
               || code.Equals("SOAP_EXCEPTION", StringComparison.OrdinalIgnoreCase)
               || code.Equals("EMPTY_RESPONSE", StringComparison.OrdinalIgnoreCase));

    private OperationalCycleWindow ValidateCycleOperationalWindow(AchCycle cycle, DateTimeOffset nowInstant)
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

        var window = _windowResolver.Resolve(
            cycle.ProcessingDate,
            cycle.StartTime,
            cycle.EndTime,
            ClearingHouseOperationalTimeZone.Resolve(cycle),
            nowInstant);
        if (!window.IsInside)
        {
            throw new InvalidOperationException($"Ciclo {cycle.Id} fuera de ventana operativa: {window.LocalStart:yyyy-MM-dd HH:mm} - {window.LocalEnd:yyyy-MM-dd HH:mm} {window.TimeZoneId}.");
        }

        return window;
    }

    private DateTime CalculateNextAttemptAtUtc(int attemptCount)
    {
        var exponent = Math.Clamp(attemptCount, 1, 5);
        var delay = TimeSpan.FromMinutes(Math.Pow(2, exponent));
        var capped = delay > TimeSpan.FromMinutes(30) ? TimeSpan.FromMinutes(30) : delay;
        return _timeProvider.GetUtcNow().UtcDateTime.Add(capped);
    }

    private sealed record ProcContrapartidasDispatchExecutionResult(
        string ResponsePayload,
        ProcContrapartidasParsedResponse ParseResult);

    private sealed record AttemptSoapAuditFields(
        string SoapMethodName,
        string SoapEndpoint,
        string ExecutionMode,
        long DurationMs,
        string SoapResponseCode,
        string SoapResponseDescription,
        string SoapTechnicalStatus,
        bool IsSuccessful,
        bool IsFunctionalRejection,
        bool IsTechnicalFailure,
        string TechnicalException);
}
