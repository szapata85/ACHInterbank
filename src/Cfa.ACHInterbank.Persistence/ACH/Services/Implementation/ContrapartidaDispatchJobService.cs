using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;
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
    private const int MaxAttempts = ContrapartidaDispatchRecovery.MaxSoapAttempts;
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
    private readonly ICycleCalendarGuard _calendarGuard;
    private readonly ICycleTransactionPolicy _cycleTransactionPolicy;

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
        IOperationalCycleWindowResolver? windowResolver = null,
        ICycleCalendarGuard? calendarGuard = null,
        ICycleTransactionPolicy? cycleTransactionPolicy = null)
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
        _calendarGuard = calendarGuard ?? new CycleCalendarGuard(context, timeProvider: _timeProvider);
        _cycleTransactionPolicy = cycleTransactionPolicy ?? new CycleTransactionPolicy(
            new ClearingHouseCyclePolicyResolver(context, new CycleNumberResolver()),
            new ClearingHouseToPaymentRailMapper(),
            new CycleNumberResolver());
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

        var effectiveCycleId = target.AchCycleId;
        if (target.State == ContrapartidaDispatchItemStateEnum.RetryPending)
        {
            var recovery = await _context.ContrapartidaDispatchItems.AsNoTracking()
                .Where(x => x.AchTransactionId == transactionId)
                .Select(x => new { x.LastErrorCode, x.RequiresReconciliation })
                .SingleAsync(ct);
            if (IsPreSendRetryCode(recovery.LastErrorCode) && !recovery.RequiresReconciliation)
            {
                effectiveCycleId = (await FindActiveRetryCycleAsync(target.ClearingHouseId, ct))?.Id
                    ?? target.AchCycleId;
            }
        }
        return await ProcessPendingAsync(
            effectiveCycleId,
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
        // Recovery is independent of the current cycle; Quartz also scans when no cycle is open.
        await ContrapartidaDispatchRecovery.RecoverStaleAsync(
            _context, _timeProvider.GetUtcNow().UtcDateTime, _logger, ct);
        // chunkSize se conserva en el contrato público; cada envío SOAP exige cardinalidad 1:1.
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

        var calendarDecision = await _calendarGuard.EnsureExecutableAsync(cycle, ct);
        if (!calendarDecision.CanExecute)
        {
            var deferredSummary = $"Ciclo {cycleId} cámara {clearingHouseId}: diferido por calendario operativo hasta {calendarDecision.RescheduledDate:yyyy-MM-dd}. SOAP no invocado.";
            _logger.LogInformation("{Summary} Reason={Reason}", deferredSummary, calendarDecision.Reason);
            return new ContrapartidaCycleDispatchResult(cycleId, clearingHouseId, 0, 0, 0, 0, 0, deferredSummary);
        }
        await EnsureCycleAllowsDebitDispatchAsync(cycle, ct);

        var nowLocal = ValidateCycleOperationalWindow(cycle, nowUtcOffset).LocalNow;

        // AUD-E2E-03: move ONLY proven pre-send recoveries onto this permitted open cycle.
        // Original monetary transaction/batch and their value date remain immutable.
        await ReassignRecoveredItemsToActiveCycleAsync(cycle, nowUtcOffset, ct);

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
                .Where(i => EligibleStates.Contains(i.State) && !i.RequiresReconciliation && i.AttemptCount < MaxAttempts)
                .Where(i => !i.NextAttemptAtUtc.HasValue || i.NextAttemptAtUtc <= nowUtc)
                .OrderBy(i => i.Id)
                .Select(i => i.Id)
                // Proc_Contrapartidas recibe una sola transacción por SOAP.
                .Take(1)
                .ToListAsync(ct);

            if (pendingItemIds.Count == 0)
            {
                break;
            }

            chunks++;
            var chunkPartialCount = 0;
            var operationToken = string.Empty;

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
            operationToken = $"scheduled:{batch.Id:N}";

            await _context.ContrapartidaDispatchItems
                .Where(i => pendingItemIds.Contains(i.Id)
                    // RES-H02-02: selection and claim must agree on the SAME cycle.
                    // The automatic recovery / manual CAS can reassign an item meanwhile.
                    && i.AchCycleId == cycle.Id && i.ClearingHouseId == cycle.ClearingHouseId
                    && i.AchCycle != null && i.AchCycle.OperationalStatus == AchCycleOperationalStatus.Open
                    && i.DispatchLeasePhase == ContrapartidaDispatchRecovery.Idle
                    && EligibleStates.Contains(i.State)
                    && i.AttemptCount < MaxAttempts
                    && !i.RequiresReconciliation
                    && (!i.NextAttemptAtUtc.HasValue || i.NextAttemptAtUtc <= nowUtc))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.State, ContrapartidaDispatchItemStateEnum.ReportingContrapartida)
                    .SetProperty(i => i.LastCorrelationId, operationToken)
                    .SetProperty(i => i.LastDispatchedBy, safeTriggeredBy)
                    .SetProperty(i => i.DispatchLeaseToken, operationToken)
                    .SetProperty(i => i.DispatchLeasePhase, ContrapartidaDispatchRecovery.Preparing)
                    .SetProperty(i => i.DispatchLeaseExpiresAtUtc,
                        (DateTime?)nowUtc.Add(ContrapartidaDispatchRecovery.LeaseDuration))
                    .SetProperty(i => i.UpdatedAt, nowUtcOffset), ct);

            var dispatchItems = await _context.ContrapartidaDispatchItems
                .Where(i => pendingItemIds.Contains(i.Id)
                    && i.State == ContrapartidaDispatchItemStateEnum.ReportingContrapartida
                    && i.DispatchLeaseToken == operationToken
                    && i.AchCycleId == cycle.Id && i.ClearingHouseId == cycle.ClearingHouseId
                    && i.DispatchLeasePhase == ContrapartidaDispatchRecovery.Preparing)
                .ToListAsync(ct);

            if (dispatchItems.Count == 0)
            {
                batch.Status = ContrapartidaDispatchBatchStatusEnum.Cancelled;
                batch.SummaryMessage = "Sin items reclamados: ciclo cambiado, reasignacion o claim de otro worker. SOAP no invocado.";
                batch.FinishedAtUtc = nowUtc;
                await _context.SaveChangesAsync(ct);
                // Stop this invocation: a cycle that closed between selection and
                // claim would otherwise be selected repeatedly in a busy loop.
                break;
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
                .Where(q => q.AchCycleId == cycle.Id && q.ClearingHouseId == cycle.ClearingHouseId && q.AchTransactionId.HasValue && transactionIds.Contains(q.AchTransactionId.Value))
                .ToListAsync(ct);
            var queueByTransactionId = queueItems
                .GroupBy(q => q.AchTransactionId)
                .ToDictionary(g => g.Key, g => g.First());

            string requestPayload = string.Empty;
            string responsePayload = string.Empty;
            string dispatchEndpoint = string.Empty;
            string executionMode = _dispatchOptions.NormalizedMode;
            string technicalException = string.Empty;
            var liveTransportAttempted = false;
            var intentPersistenceAttempted = false;
            var intentCommitted = false;
            var preSoapAbortAttempted = false;
            var preSoapAborted = false;
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

                // First boundary: the mapper may have taken long enough for a cycle to close.
                var itemIds = dispatchItems.Select(x => x.Id).ToArray();
                if (!await IsCycleAndClaimExecutableBeforeSoapAsync(cycle.Id, cycle.ClearingHouseId,
                        itemIds, operationToken, ContrapartidaDispatchRecovery.Preparing, ct))
                {
                    preSoapAbortAttempted = true;
                    await ContrapartidaDispatchRecovery.AbortUnsentCycleClaimAsync(
                        _context, itemIds, operationToken, batch.Id, cycle.Id,
                        ContrapartidaDispatchRecovery.Preparing, _timeProvider.GetUtcNow().UtcDateTime,
                        requestPayload, dispatchEndpoint, executionMode, ct);
                    preSoapAborted = true;
                }
                else
                {
                    // Persist InFlight BEFORE I/O. A crash from this point is uncertain.
                    intentPersistenceAttempted = true;
                    await ContrapartidaDispatchRecovery.PersistIntentBeforeTransportAsync(
                        _context, itemIds, operationToken, batch.Id,
                        _timeProvider.GetUtcNow().UtcDateTime, requestPayload, Hash(requestPayload),
                        resolution.MappingSetId, resolution.MappingVersion, resolution.MappingSnapshotHash,
                        dispatchEndpoint, executionMode, ct);
                    intentCommitted = true;
                    // Second, last-moment fence AFTER evidence commit and IMMEDIATELY before SOAP.
                    // It re-reads persisted cycle, calendar, policy, window, lease AND cycle ID.
                    if (!await IsCycleAndClaimExecutableBeforeSoapAsync(cycle.Id, cycle.ClearingHouseId,
                            itemIds, operationToken, ContrapartidaDispatchRecovery.InFlight, ct))
                    {
                        preSoapAbortAttempted = true;
                        await ContrapartidaDispatchRecovery.AbortUnsentCycleClaimAsync(
                            _context, itemIds, operationToken, batch.Id, cycle.Id,
                            ContrapartidaDispatchRecovery.InFlight, _timeProvider.GetUtcNow().UtcDateTime,
                            requestPayload, dispatchEndpoint, executionMode, ct);
                        preSoapAborted = true;
                    }
                    else
                    {
                        liveTransportAttempted = IsLiveMode(executionMode);
                        var dispatchResult = await DispatchProcContrapartidasAsync(
                            requestPayload, cycle.Id, cycle.ClearingHouseId, executionMode, ct);
                        responsePayload = dispatchResult.ResponsePayload;
                        parseResult = dispatchResult.ParseResult;
                    }
                }
            }
            catch (Exception) when (preSoapAbortAttempted)
            {
                // Do not manufacture a SOAP attempt when a DB-only abort fails.
                // Orphan recovery will keep any InFlight state in reconciliation.
                _context.ChangeTracker.Clear();
                throw;
            }
            catch (Exception) when (intentPersistenceAttempted && !intentCommitted)
            {
                // The database may or may not have committed; never call SOAP or overwrite a peer's claim.
                _context.ChangeTracker.Clear();
                throw;
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
                    IsRetryable: false,
                    IsFunctionalRejection: false,
                    ErrorCode: "SOAP_EXCEPTION",
                    ErrorMessage: "La respuesta del servicio es incierta y requiere conciliación antes de reenviar.",
                    RawResponse: technicalException,
                    ResponseCode: "SOAP_EXCEPTION",
                    ItemResults: new Dictionary<int, ProcContrapartidasParsedItemResponse>());
                responsePayload = technicalException;
            }

            if (preSoapAborted)
            {
                // The item stays RetryPending with an audit entry NotExecuted (negative
                // recovery number); no monetary attempt was consumed or sent.
                foreach (var item in dispatchItems) await _context.Entry(item).ReloadAsync(ct);
                batch.Status = ContrapartidaDispatchBatchStatusEnum.Cancelled;
                batch.FinishedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
                batch.SummaryMessage = "RES-H02-01: ciclo/claim no ejecutable antes del SOAP; envío omitido, pendiente de nuevo ciclo.";
                _logger.LogWarning("{Summary} CycleId={CycleId} BatchId={BatchId}",
                    batch.SummaryMessage, cycle.Id, batch.Id);
                await _context.SaveChangesAsync(ct);
                break;
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
                var requiresReconciliation = RequiresReconciliation(transportStatus, parseResult, technicalException, liveTransportAttempted)
                    || (liveTransportAttempted && (catalogResult is null || !catalogResult.IsKnownCode));
                var retryAllowed = !requiresReconciliation && (catalogResult is null
                    ? (hasItemResult ? txResult!.IsRetryable : parseResult.IsRetryable)
                    : catalogResult.RetryAllowed);
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
                    RequiresManualReview = requiresReconciliation || catalogResult?.RequiresManualReview == true,
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
                        RequiresManualReview = requiresReconciliation || catalogResult?.RequiresManualReview == true,
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
                item.RequiresReconciliation = requiresReconciliation || catalogResult?.RequiresManualReview == true;
                item.DispatchLeaseExpiresAtUtc = null;
                item.DispatchLeasePhase = ContrapartidaDispatchRecovery.Idle;
                item.DispatchLeaseToken = string.Empty;
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
        // request.ChunkSize se conserva para compatibilidad; cada envío SOAP procesa un ítem.
        var retryJobId = $"manual-retry:{request.SourceBatchId:N}";

        var conflictingRetryInProgress = await _context.ContrapartidaDispatchBatches
            .AsNoTracking()
            .AnyAsync(x => x.JobId == retryJobId && x.Status == ContrapartidaDispatchBatchStatusEnum.Processing, ct);
        if (conflictingRetryInProgress)
        {
            throw new InvalidOperationException("Ya existe un reintento manual en ejecución para este batch origen.");
        }

        // H02: the source batch may belong to a closed or expired cycle. Select a
        // currently open, calendar-valid, debit-permitted cycle using the same policy
        // as the automatic pre-send recovery; never fall back to a closed source.
        var sourceCycle = await _context.AchCycles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sourceBatch.AchCycleId
                && x.ClearingHouseId == sourceBatch.ClearingHouseId, ct)
            ?? throw new InvalidOperationException($"No existe ciclo {sourceBatch.AchCycleId} para cámara {sourceBatch.ClearingHouseId}.");
        var cycle = await FindActiveRetryCycleAsync(sourceBatch.ClearingHouseId, ct,
            sourceCycle.ProcessingDate.Date);
        if (cycle is null)
        {
            var summary = $"Reintento {sourceBatch.Id}: diferido; no hay ciclo abierto con calendario y débitos permitidos. SOAP no invocado.";
            _logger.LogInformation("{Summary}", summary);
            return new ContrapartidaBatchRetryResult(
                sourceBatch.Id, Guid.Empty, sourceBatch.AchCycleId, sourceBatch.ClearingHouseId,
                0, 0, 0, 0, 0, summary);
        }
        // Re-evaluate at the boundary: no manual debit outside the permitted window.
        var calendarDecision = await _calendarGuard.EnsureExecutableAsync(cycle, ct);
        if (!calendarDecision.CanExecute)
        {
            var summary = $"Reintento {sourceBatch.Id}: diferido por calendario operativo. SOAP no invocado.";
            return new ContrapartidaBatchRetryResult(
                sourceBatch.Id, Guid.Empty, sourceBatch.AchCycleId, sourceBatch.ClearingHouseId,
                0, 0, 0, 0, 0, summary);
        }
        await EnsureCycleAllowsDebitDispatchAsync(cycle, ct);
        var nowUtcOffset = _timeProvider.GetUtcNow();
        var nowUtc = nowUtcOffset.UtcDateTime;
        _ = ValidateCycleOperationalWindow(cycle, nowUtcOffset);

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
            .Where(i => i.AchCycle != null
                && i.AchCycle.ProcessingDate.Date <= cycle.ProcessingDate.Date)
            .Where(i => i.AchTransaction.ClassificationStatus == AchTransactionClassificationStatus.Determined
                && i.AchTransaction.MonetaryIntegrationRoute == AchMonetaryIntegrationRoute.ProcContrapartidas
                && i.AchTransaction.Direction == AchTransactionDirection.Outgoing
                && i.AchTransaction.Origin == AchTransactionOrigin.Cfa
                && i.AchTransaction.Type == TransactionTypeEnum.Debit)
            .Select(i => new { i.Id, i.State, i.AttemptCount, i.RequiresReconciliation,
                i.AchCycleId, i.DispatchLeaseToken, i.DispatchLeasePhase,
                i.LastReassignedFromCycleId, i.LastReassignedAtUtc })
            .ToListAsync(ct);

        var lastAttempts = await _context.ContrapartidaDispatchAttempts
            .AsNoTracking()
            .Where(a => sourceItemIds.Contains(a.DispatchItemId))
            .GroupBy(a => a.DispatchItemId)
            .Select(g => g.OrderByDescending(x => x.AttemptNumber)
                .Select(x => new { x.DispatchItemId, x.RetryEligible, x.RequiresManualReview, x.IsSuccessful, x.TransportStatus, x.SoapResponseCode })
                .First())
            .ToListAsync(ct);

        var lastAttemptByItemId = lastAttempts.ToDictionary(x => x.DispatchItemId);

        var selectedItemIds = items
            .Where(i =>
            {
                if (i.AttemptCount >= MaxAttempts
                    || i.DispatchLeasePhase != ContrapartidaDispatchRecovery.Idle
                    || i.RequiresReconciliation
                    || i.State == ContrapartidaDispatchItemStateEnum.ReportedToContrapartida
                    || (lastAttemptByItemId.TryGetValue(i.Id, out var lastAttempt)
                        && (lastAttempt.IsSuccessful || lastAttempt.RequiresManualReview
                            || lastAttempt.TransportStatus == IntegrationTransportStatus.TimedOut
                            || lastAttempt.SoapResponseCode == "SOAP_EXCEPTION"
                            || lastAttempt.SoapResponseCode == "PARSER_ERROR")))
                {
                    return false;
                }

                if (request.Scope == ContrapartidaDispatchRetryScope.Full)
                {
                    return i.State != ContrapartidaDispatchItemStateEnum.ReportingContrapartida
                        && i.State != ContrapartidaDispatchItemStateEnum.Retrying;
                }

                return lastAttemptByItemId.TryGetValue(i.Id, out var eligibleAttempt)
                    && eligibleAttempt.RetryEligible;
            })
            .Select(i => i.Id)
            .ToList();

        if (selectedItemIds.Count == 0)
        {
            throw new InvalidOperationException("No existen items elegibles para reintento según alcance solicitado.");
        }

        var processingBatch = new ContrapartidaDispatchBatch
        {
            AchCycleId = cycle.Id,
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
            // A large manual batch may span a cycle boundary: revalidate the DB state,
            // calendar, and operational window before claiming EACH monetary instruction.
            // Unprocessed items stay eligible for a later authorised cycle.
            var currentInstant = _timeProvider.GetUtcNow();
            var currentCycle = await _context.AchCycles.AsNoTracking()
                .Include(c => c.ClearingHouse).ThenInclude(ch => ch!.ClearingHouseConfig)
                .Include(c => c.ClearingHouseCycleConfig)
                .SingleOrDefaultAsync(c => c.Id == cycle.Id
                    && c.ClearingHouseId == cycle.ClearingHouseId, ct);
            if (currentCycle is null
                || currentCycle.OperationalStatus != AchCycleOperationalStatus.Open
                || !_windowResolver.Resolve(currentCycle.ProcessingDate, currentCycle.StartTime,
                    currentCycle.EndTime, ClearingHouseOperationalTimeZone.Resolve(currentCycle),
                    currentInstant).IsInside
                || !(await _calendarGuard.EnsureExecutableAsync(currentCycle, ct)).CanExecute)
            {
                _logger.LogWarning("H02 manual retry deferred: cycle {CycleId} is no longer executable", cycle.Id);
                break;
            }
            DateTime currentLocal;
            try
            {
                await EnsureCycleAllowsDebitDispatchAsync(currentCycle, ct);
                currentLocal = ValidateCycleOperationalWindow(currentCycle, currentInstant).LocalNow;
            }
            catch (InvalidOperationException ex)
            {
                // A policy/window switch between two items must not strand a manual
                // batch in Processing or start an unauthorised SOAP request.
                _logger.LogWarning(ex, "H02 manual retry deferred: cycle {CycleId} no longer permits debits", cycle.Id);
                break;
            }
            // Mantener lote operativo, pero aislar cada request y su respuesta.
            var chunkIds = selectedItemIds.Take(1).ToList();
            selectedItemIds.RemoveRange(0, chunkIds.Count);

            var selected = items.Single(i => i.Id == chunkIds[0]);
            // H02: do not claim if another worker has already changed the state,
            // lease, cycle, or attempt counter. Reassignment and claim are one CAS.
            var affected = await _context.ContrapartidaDispatchItems
                .Where(i => i.Id == selected.Id
                    && i.AchCycleId == selected.AchCycleId
                    && i.State == selected.State
                    && i.AttemptCount == selected.AttemptCount
                    && i.DispatchLeaseToken == selected.DispatchLeaseToken
                    && i.DispatchLeasePhase == ContrapartidaDispatchRecovery.Idle
                    && !i.RequiresReconciliation && i.AttemptCount < MaxAttempts)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.AchCycleId, cycle.Id)
                    .SetProperty(i => i.LastReassignedFromCycleId,
                        selected.AchCycleId != cycle.Id ? selected.AchCycleId : selected.LastReassignedFromCycleId)
                    .SetProperty(i => i.LastReassignedAtUtc,
                        selected.AchCycleId != cycle.Id ? (DateTime?)nowUtc : selected.LastReassignedAtUtc)
                    .SetProperty(i => i.State, ContrapartidaDispatchItemStateEnum.Retrying)
                    .SetProperty(i => i.LastCorrelationId, operationToken)
                    .SetProperty(i => i.LastDispatchedBy, safeTriggeredBy)
                    .SetProperty(i => i.DispatchLeaseToken, operationToken)
                    .SetProperty(i => i.DispatchLeasePhase, ContrapartidaDispatchRecovery.Preparing)
                    .SetProperty(i => i.DispatchLeaseExpiresAtUtc,
                        (DateTime?)currentInstant.UtcDateTime.Add(ContrapartidaDispatchRecovery.LeaseDuration))
                    .SetProperty(i => i.UpdatedAt, currentInstant), ct);
            if (affected != 1) continue;

            var claimed = await _context.ContrapartidaDispatchItems
                .Where(i => chunkIds.Contains(i.Id)
                    && i.AchCycleId == cycle.Id
                    && i.State == ContrapartidaDispatchItemStateEnum.Retrying
                    && i.DispatchLeasePhase == ContrapartidaDispatchRecovery.Preparing
                    && i.DispatchLeaseToken == operationToken)
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

            var startedAtUtc = currentInstant.UtcDateTime;
            string requestPayload = string.Empty;
            string responsePayload = string.Empty;
            string dispatchEndpoint = string.Empty;
            string executionMode = _dispatchOptions.NormalizedMode;
            string technicalException = string.Empty;
            var liveTransportAttempted = false;
            var intentPersistenceAttempted = false;
            var intentCommitted = false;
            var preSoapAbortAttempted = false;
            var preSoapAborted = false;
            ProcContrapartidasParsedResponse? parseResult = null;
            try
            {
                await EnsureContrapartidasReadinessAsync(txs, ct);
                var resolution = await _procContrapartidasRequestMapper.ResolveAsync(currentCycle, txs, currentLocal, ct);
                EnsureNoFallbackResolution(resolution);
                requestPayload = _procContrapartidasRequestMapper.BuildSoapBody(resolution.Contract);
                var runtime = await ResolveProcContrapartidasRuntimeAsync(ct);
                dispatchEndpoint = runtime.Endpoint;
                executionMode = runtime.OperatingMode;
                processingBatch.MappingSetId = resolution.MappingSetId;
                processingBatch.MappingVersion = resolution.MappingVersion;
                processingBatch.MappingSnapshotHash = resolution.MappingSnapshotHash;
                var itemIds = claimed.Select(x => x.Id).ToArray();
                if (!await IsCycleAndClaimExecutableBeforeSoapAsync(cycle.Id, cycle.ClearingHouseId,
                        itemIds, operationToken, ContrapartidaDispatchRecovery.Preparing, ct))
                {
                    preSoapAbortAttempted = true;
                    await ContrapartidaDispatchRecovery.AbortUnsentCycleClaimAsync(
                        _context, itemIds, operationToken, processingBatch.Id, cycle.Id,
                        ContrapartidaDispatchRecovery.Preparing, _timeProvider.GetUtcNow().UtcDateTime,
                        requestPayload, dispatchEndpoint, executionMode, ct);
                    preSoapAborted = true;
                }
                else
                {
                    intentPersistenceAttempted = true;
                    await ContrapartidaDispatchRecovery.PersistIntentBeforeTransportAsync(
                        _context, itemIds, operationToken, processingBatch.Id,
                        _timeProvider.GetUtcNow().UtcDateTime, requestPayload, Hash(requestPayload),
                        resolution.MappingSetId, resolution.MappingVersion, resolution.MappingSnapshotHash,
                        dispatchEndpoint, executionMode, ct);
                    intentCommitted = true;
                    if (!await IsCycleAndClaimExecutableBeforeSoapAsync(cycle.Id, cycle.ClearingHouseId,
                            itemIds, operationToken, ContrapartidaDispatchRecovery.InFlight, ct))
                    {
                        preSoapAbortAttempted = true;
                        await ContrapartidaDispatchRecovery.AbortUnsentCycleClaimAsync(
                            _context, itemIds, operationToken, processingBatch.Id, cycle.Id,
                            ContrapartidaDispatchRecovery.InFlight, _timeProvider.GetUtcNow().UtcDateTime,
                            requestPayload, dispatchEndpoint, executionMode, ct);
                        preSoapAborted = true;
                    }
                    else
                    {
                        liveTransportAttempted = IsLiveMode(executionMode);
                        var dispatchResult = await DispatchProcContrapartidasAsync(
                            requestPayload, cycle.Id, cycle.ClearingHouseId, executionMode, ct);
                        responsePayload = dispatchResult.ResponsePayload;
                        parseResult = dispatchResult.ParseResult;
                    }
                }
            }
            catch (Exception) when (preSoapAbortAttempted)
            {
                _context.ChangeTracker.Clear();
                throw;
            }
            catch (Exception) when (intentPersistenceAttempted && !intentCommitted)
            {
                _context.ChangeTracker.Clear();
                throw;
            }
            catch (Exception ex)
            {
                technicalException = BuildTechnicalException(ex);
                parseResult = new ProcContrapartidasParsedResponse(
                    IsSuccess: false,
                    IsSoapFault: false,
                    IsRetryable: false,
                    IsFunctionalRejection: false,
                    ErrorCode: "SOAP_EXCEPTION",
                    ErrorMessage: "La respuesta del servicio es incierta y requiere conciliación antes de reenviar.",
                    RawResponse: technicalException,
                    ResponseCode: "SOAP_EXCEPTION",
                    ItemResults: new Dictionary<int, ProcContrapartidasParsedItemResponse>());
                responsePayload = technicalException;
            }

            if (preSoapAborted)
            {
                foreach (var item in claimed) await _context.Entry(item).ReloadAsync(ct);
                _logger.LogWarning("RES-H02-01 manual retry: cycle {CycleId} closed or claim moved before SOAP; batch {BatchId} stops without sending", cycle.Id, processingBatch.Id);
                break;
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
                var requiresReconciliation = RequiresReconciliation(transportStatus, parseResult, technicalException, liveTransportAttempted)
                    || (liveTransportAttempted && (catalogResult is null || !catalogResult.IsKnownCode));
                var retryAllowed = !requiresReconciliation && (catalogResult is null
                    ? (hasItemResult ? txResult!.IsRetryable : parseResult.IsRetryable)
                    : catalogResult.RetryAllowed);
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
                    RequiresManualReview = requiresReconciliation || catalogResult?.RequiresManualReview == true,
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
                item.RequiresReconciliation = requiresReconciliation || catalogResult?.RequiresManualReview == true;
                item.DispatchLeaseExpiresAtUtc = null;
                item.DispatchLeasePhase = ContrapartidaDispatchRecovery.Idle;
                item.DispatchLeaseToken = string.Empty;
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

            // Los XML individuales quedan en cada intento; el lote con múltiples ítems no representa una llamada SOAP única.
            if (processingBatch.TotalItems == 1)
            {
                processingBatch.RequestPayloadXml = requestPayload;
                processingBatch.ResponsePayloadXml = responsePayload;
            }
            await _context.SaveChangesAsync(ct);
        }

        processingBatch.TotalSucceeded = totalSucceeded;
        processingBatch.TotalFailed = totalFailed;
        processingBatch.TotalPartial = totalPartial;
        processingBatch.FinishedAtUtc = nowUtc;
        processingBatch.Status = totalProcessed == 0
            ? ContrapartidaDispatchBatchStatusEnum.Cancelled
            : totalProcessed < processingBatch.TotalItems
                ? ContrapartidaDispatchBatchStatusEnum.CompletedWithErrors
                : totalFailed == 0
                    ? ContrapartidaDispatchBatchStatusEnum.Completed
                    : totalSucceeded > 0
                        ? ContrapartidaDispatchBatchStatusEnum.CompletedWithErrors
                        : ContrapartidaDispatchBatchStatusEnum.Failed;
        processingBatch.SummaryMessage = $"Retry source={request.SourceBatchId} scope={request.Scope} selected={processingBatch.TotalItems} processed={totalProcessed} pending={processingBatch.TotalItems - totalProcessed} success={totalSucceeded} failed={totalFailed}";

        await _context.SaveChangesAsync(ct);

        return new ContrapartidaBatchRetryResult(
            request.SourceBatchId,
            processingBatch.Id,
            cycle.Id,
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

    private static bool RequiresReconciliation(
        IntegrationTransportStatus status,
        ProcContrapartidasParsedResponse parsed,
        string technicalException,
        bool liveTransportAttempted)
        => status == IntegrationTransportStatus.TimedOut
            || (liveTransportAttempted && status == IntegrationTransportStatus.Failed)
            || (liveTransportAttempted && !string.IsNullOrWhiteSpace(technicalException))
            || (liveTransportAttempted && (parsed.ErrorMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                || parsed.ResponseCode.Equals("PARSER_ERROR", StringComparison.OrdinalIgnoreCase)
                || parsed.ResponseCode.Equals("EMPTY_RESPONSE", StringComparison.OrdinalIgnoreCase)));

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

    // A recovered pre-send debit cannot use an expired source cycle. No already-sent,
    // unknown, or manually reviewed instruction is eligible for re-routing.
    private async Task ReassignRecoveredItemsToActiveCycleAsync(
        AchCycle target, DateTimeOffset nowInstant, CancellationToken ct)
    {
        var candidates = await _context.ContrapartidaDispatchItems.AsNoTracking()
            .Include(x => x.AchCycle)
            .Where(x => x.ClearingHouseId == target.ClearingHouseId
                && x.AchCycleId != target.Id
                && x.State == ContrapartidaDispatchItemStateEnum.RetryPending
                && (x.LastErrorCode == "E2E02_PRE_SEND_CRASH"
                    || x.LastErrorCode == ContrapartidaDispatchRecovery.UnsentCycleCode)
                && !x.RequiresReconciliation
                && x.DispatchLeasePhase == ContrapartidaDispatchRecovery.Idle
                && x.AttemptCount < MaxAttempts
                && (!x.NextAttemptAtUtc.HasValue || x.NextAttemptAtUtc <= nowInstant.UtcDateTime))
            .OrderBy(x => x.Id).Take(100).ToListAsync(ct);
        foreach (var item in candidates)
        {
            var source = item.AchCycle;
            if (source is null || target.ProcessingDate.Date < source.ProcessingDate.Date) continue;
            var sourceWindow = _windowResolver.Resolve(source.ProcessingDate, source.StartTime,
                source.EndTime, ClearingHouseOperationalTimeZone.Resolve(target), nowInstant);
            if (source.OperationalStatus == AchCycleOperationalStatus.Open && sourceWindow.IsInside)
                continue;

            var changed = await _context.ContrapartidaDispatchItems
                .Where(x => x.Id == item.Id
                    && x.AchCycleId == item.AchCycleId
                    && x.ClearingHouseId == target.ClearingHouseId
                    && x.State == ContrapartidaDispatchItemStateEnum.RetryPending
                    && (x.LastErrorCode == "E2E02_PRE_SEND_CRASH"
                        || x.LastErrorCode == ContrapartidaDispatchRecovery.UnsentCycleCode)
                    && x.DispatchLeaseToken == item.DispatchLeaseToken
                    && x.DispatchLeasePhase == ContrapartidaDispatchRecovery.Idle
                    && !x.RequiresReconciliation
                    && x.AttemptCount == item.AttemptCount)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.AchCycleId, target.Id)
                    .SetProperty(x => x.LastReassignedFromCycleId, item.AchCycleId)
                    .SetProperty(x => x.LastReassignedAtUtc, (DateTime?)nowInstant.UtcDateTime)
                    .SetProperty(x => x.LastDispatchedBy, "recovery:cycle-reassigned")
                    .SetProperty(x => x.UpdatedAt, nowInstant), ct);
            if (changed == 1)
                _logger.LogWarning("AUD-E2E-03 item {ItemId}: pre-send retry re-routed {FromCycle} -> {ToCycle}",
                    item.Id, item.AchCycleId, target.Id);
        }
    }

    private static bool IsPreSendRetryCode(string? code)
        => code == "E2E02_PRE_SEND_CRASH" || code == ContrapartidaDispatchRecovery.UnsentCycleCode;

    // RES-H02-01/02: both scheduled and manual paths share a fresh DB-side final
    // preflight. The final conditional UPDATE is the claim/cycle fence; a stale
    // cycle object from mapping cannot authorise a SOAP debit.
    private async Task<bool> IsCycleAndClaimExecutableBeforeSoapAsync(
        string cycleId, int clearingHouseId, IReadOnlyList<long> itemIds,
        string token, string expectedPhase, CancellationToken ct)
    {
        if (itemIds.Count != 1) return false;
        var fresh = await _context.AchCycles.AsNoTracking()
            .Include(x => x.ClearingHouse).ThenInclude(x => x!.ClearingHouseConfig)
            .Include(x => x.ClearingHouseCycleConfig)
            .SingleOrDefaultAsync(x => x.Id == cycleId && x.ClearingHouseId == clearingHouseId, ct);
        if (fresh is null || fresh.OperationalStatus != AchCycleOperationalStatus.Open)
            return false;
        var instant = _timeProvider.GetUtcNow();
        try
        {
            _ = ValidateCycleOperationalWindow(fresh, instant);
            if (!(await _calendarGuard.EnsureExecutableAsync(fresh, ct)).CanExecute)
                return false;
            await EnsureCycleAllowsDebitDispatchAsync(fresh, ct);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "RES-H02-01 cycle {CycleId} rejected at SOAP preflight", cycleId);
            return false;
        }

        // Single SQL CAS prevents a claim from belonging to another cycle or a
        // recovered worker. Requiring persisted Open status closes the selection
        // /claim race for both providers; lease and phase remain in the predicate.
        var fenced = await _context.ContrapartidaDispatchItems
            .Where(x => itemIds.Contains(x.Id)
                && x.AchCycleId == cycleId && x.ClearingHouseId == clearingHouseId
                && x.AchCycle != null && x.AchCycle.OperationalStatus == AchCycleOperationalStatus.Open
                && x.DispatchLeaseToken == token && x.DispatchLeasePhase == expectedPhase
                && x.DispatchLeaseExpiresAtUtc > instant.UtcDateTime
                && !x.RequiresReconciliation
                && (x.State == ContrapartidaDispatchItemStateEnum.ReportingContrapartida
                    || x.State == ContrapartidaDispatchItemStateEnum.Retrying))
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.UpdatedAt, instant), ct);
        return fenced == itemIds.Count;
    }

    private async Task<AchCycle?> FindActiveRetryCycleAsync(
        int clearingHouseId, CancellationToken ct, DateTime? minimumProcessingDate = null)
    {
        var nowInstant = _timeProvider.GetUtcNow();
        var cycles = await _context.AchCycles.AsNoTracking()
            .Include(x => x.ClearingHouse).ThenInclude(x => x!.ClearingHouseConfig)
            .Include(x => x.ClearingHouseCycleConfig)
            .Where(x => x.ClearingHouseId == clearingHouseId
                && x.OperationalStatus == AchCycleOperationalStatus.Open)
            .OrderBy(x => x.ProcessingDate).ThenBy(x => x.EndTime).ToListAsync(ct);
        foreach (var cycle in cycles)
        {
            if (minimumProcessingDate.HasValue
                && cycle.ProcessingDate.Date < minimumProcessingDate.Value.Date)
                continue;
            if (cycle.ClearingHouse is null || !cycle.ClearingHouse.IsActive ||
                (cycle.ClearingHouseCycleConfig is not null && !cycle.ClearingHouseCycleConfig.IsActive))
                continue;
            var config = cycle.ClearingHouseCycleConfig;
            if (config is not null && (cycle.ProcessingDate.Date < config.EffectiveFrom.Date ||
                (config.EffectiveTo.HasValue && cycle.ProcessingDate.Date > config.EffectiveTo.Value.Date)))
                continue;
            if (!_windowResolver.Resolve(cycle.ProcessingDate, cycle.StartTime, cycle.EndTime,
                    ClearingHouseOperationalTimeZone.Resolve(cycle), nowInstant).IsInside)
                continue;
            if (!(await _calendarGuard.EnsureExecutableAsync(cycle, ct)).CanExecute)
                continue;
            var decision = await _cycleTransactionPolicy.EvaluateAsync(new CycleTransactionPolicyRequest(
                cycle.ClearingHouseId, cycle.ClearingHouseCycleConfigId, cycle.ProcessingDate,
                cycle.ClearingHouse.Code, cycle.ClearingHouse.ClearingHouseConfig?.PaymentRailCode,
                cycle.CycleName, TransactionTypeEnum.Debit, false), ct);
            if (decision.IsAllowed) return cycle;
        }
        return null;
    }

    private OperationalCycleWindow ValidateCycleOperationalWindow(AchCycle cycle, DateTimeOffset nowInstant)
    {
        if (cycle.ClearingHouse is null || !cycle.ClearingHouse.IsActive)
        {
            throw new InvalidOperationException($"Cámara inactiva o no disponible para {cycle.Id}.");
        }

        if (cycle.OperationalStatus != AchCycleOperationalStatus.Open)
        {
            throw new InvalidOperationException($"El ciclo {cycle.Id} no se encuentra abierto.");
        }

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

    private async Task EnsureCycleAllowsDebitDispatchAsync(AchCycle cycle, CancellationToken ct)
    {
        var decision = await _cycleTransactionPolicy.EvaluateAsync(new CycleTransactionPolicyRequest(
            cycle.ClearingHouseId,
            cycle.ClearingHouseCycleConfigId,
            cycle.ProcessingDate,
            cycle.ClearingHouse?.Code,
            cycle.ClearingHouse?.ClearingHouseConfig?.PaymentRailCode,
            cycle.CycleName,
            TransactionTypeEnum.Debit,
            false), ct);
        if (!decision.IsAllowed)
        {
            throw new InvalidOperationException($"{decision.ReasonCode}: {decision.Message}");
        }
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
