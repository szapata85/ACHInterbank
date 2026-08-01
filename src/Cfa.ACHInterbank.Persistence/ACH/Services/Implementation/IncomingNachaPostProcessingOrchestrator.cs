using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaPostProcessingOrchestrator : IIncomingNachaPostProcessingOrchestrator
{
    // The scheduler and an authorized operator may both request a local run.
    // Serialize them in-process so a queued entry is never dispatched twice
    // before its first execution has persisted its terminal state.
    private static readonly SemaphoreSlim DispatchGate = new(1, 1);
    private const string SoapMethodName = IntegrationGuaranteeConstants.ProcTransacciones;
    private const string TechnicalStatusSucceeded = "Succeeded";
    private const string TechnicalStatusFunctionalRejection = "FunctionalRejection";
    private const string TechnicalStatusSoapFault = "SoapFault";
    private const string TechnicalStatusRetryableFailure = "RetryableFailure";
    private const string TechnicalStatusParserError = "ParserError";
    private const string TechnicalStatusTechnicalException = "TechnicalException";
    private const string TechnicalStatusDryRun = "DryRun";
    private const string TechnicalStatusDisabled = "Disabled";
    private const string TechnicalStatusUnknownFailure = "UnknownFailure";

    private readonly AchDbContext _context;
    private readonly IProcTransaccionesRequestMapper _mapper;
    private readonly IProcTransaccionesResponseParser _parser;
    private readonly IWscfaachSoapClient _soapClient;
    private readonly IncomingNachaDispatchResilienceOptions _resilienceOptions;
    private readonly ProcTransaccionesDispatchOptions _dispatchOptions;
    private readonly ITransactionIntegrationOperationResolver? _operationResolver;
    private readonly IIntegrationMappingReadinessService? _mappingReadinessService;
    private readonly IIntegrationMappingTraceWriter? _mappingTraceWriter;
    private readonly IContrapartidaDispatchJobService? _contrapartidaDispatchJobService;
    private readonly ISoapIntegrationSettingsService? _soapIntegrationSettingsService;
    private readonly IIncomingNachaLocalLivePreparationService? _localLivePreparationService;
    private readonly IIntegrationResponseCatalogResolver? _responseCatalogResolver;
    private readonly TimeProvider _timeProvider;
    private readonly IIncomingNachaAchResultResolver? _achResultResolver;

    public IncomingNachaPostProcessingOrchestrator(
        AchDbContext context,
        IProcTransaccionesRequestMapper mapper,
        IProcTransaccionesResponseParser parser,
        IWscfaachSoapClient soapClient,
        IOptions<IncomingNachaDispatchResilienceOptions>? resilienceOptions = null,
        IOptions<ProcTransaccionesDispatchOptions>? dispatchOptions = null,
        ITransactionIntegrationOperationResolver? operationResolver = null,
        IIntegrationMappingReadinessService? mappingReadinessService = null,
        IIntegrationMappingTraceWriter? mappingTraceWriter = null,
        IContrapartidaDispatchJobService? contrapartidaDispatchJobService = null,
        ISoapIntegrationSettingsService? soapIntegrationSettingsService = null,
        IIncomingNachaLocalLivePreparationService? localLivePreparationService = null,
        IIntegrationResponseCatalogResolver? responseCatalogResolver = null,
        TimeProvider? timeProvider = null,
        IIncomingNachaAchResultResolver? achResultResolver = null)
    {
        _context = context;
        _mapper = mapper;
        _parser = parser;
        _soapClient = soapClient;
        _resilienceOptions = resilienceOptions?.Value ?? new IncomingNachaDispatchResilienceOptions();
        _dispatchOptions = dispatchOptions?.Value ?? new ProcTransaccionesDispatchOptions();
        _operationResolver = operationResolver;
        _mappingReadinessService = mappingReadinessService;
        _mappingTraceWriter = mappingTraceWriter;
        _contrapartidaDispatchJobService = contrapartidaDispatchJobService;
        _soapIntegrationSettingsService = soapIntegrationSettingsService;
        _localLivePreparationService = localLivePreparationService;
        _responseCatalogResolver = responseCatalogResolver;
        _timeProvider = timeProvider ?? OperationalTimeProvider.SystemBogota;
        _achResultResolver = achResultResolver;
    }

    public async Task<IncomingNachaPostProcessingRunResult> ExecuteAsync(
        int chunkSize,
        string triggeredBy,
        CancellationToken ct = default)
    {
        await DispatchGate.WaitAsync(ct);
        try
        {
            return await ExecuteCoreAsync(chunkSize, triggeredBy, ct);
        }
        finally
        {
            DispatchGate.Release();
        }
    }

    private async Task<IncomingNachaPostProcessingRunResult> ExecuteCoreAsync(
        int chunkSize,
        string triggeredBy,
        CancellationToken ct)
    {
        var safeChunk = Math.Clamp(chunkSize, 10, 500);
        var nowUtcOffset = _timeProvider.GetUtcNow();
        var nowUtc = nowUtcOffset.UtcDateTime;
        var nowLocal = TimeZoneInfo.ConvertTime(nowUtcOffset, _timeProvider.LocalTimeZone).DateTime;
        var integrationCodePolicies = await _context.AchFileRejectionCodes
            .AsNoTracking()
            .Where(x => x.IsActive && x.AppliesToStage == "Integration")
            .ToDictionaryAsync(x => x.Code.ToUpper(), x => x.IsRetryable, ct);

        var waitingWindowResolution = await ReevaluateWaitingWindowAsync(nowUtc, nowLocal, ct);

        var ids = await _context.IncomingNachaDispatchQueue
            .AsNoTracking()
            .Where(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.Queued || x.QueueStatus == IncomingNachaDispatchQueueStatus.RetryPending)
            .Where(x => !x.NextAttemptAtUtc.HasValue || x.NextAttemptAtUtc <= nowUtc)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .Take(safeChunk)
            .ToListAsync(ct);

        if (ids.Count == 0)
        {
            return new IncomingNachaPostProcessingRunResult(
                0,
                0,
                0,
                0,
                0,
                waitingWindowResolution.Blocked,
                waitingWindowResolution.Released,
                "Sin elementos en cola.");
        }

        var queues = await _context.IncomingNachaDispatchQueue
            .Include(x => x.AchTransaction).ThenInclude(x => x.AchCycle)
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(ct);
        var ingestions = await _context.IncomingNachaFileIngestions
            .Where(x => queues.Select(q => q.IncomingNachaFileIngestionId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
        var classifications = await _context.IncomingNachaEntryClassifications
            .Where(x => queues.Select(q => q.IncomingNachaEntryClassificationId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        var confirmed = 0;
        var retryPending = 0;
        var failedFinal = 0;
        var blocked = waitingWindowResolution.Blocked;
        var waitingWindow = waitingWindowResolution.Released;
        var contrapartidaDispatchTargets = new HashSet<(string CycleId, int ClearingHouseId)>();
        var procTransaccionesRuntime = await ResolveProcTransaccionesRuntimeAsync(ct);

        foreach (var queue in queues)
        {
            queue.QueueStatus = IncomingNachaDispatchQueueStatus.Dispatching;
            queue.LastAttemptAtUtc = nowUtc;
            queue.AttemptCount += 1;
            AddAutomaticEvent(queue, "DispatchStarted", "Applied", "Dispatch automático iniciado.");

            var correlationId = $"in-nacha-{queue.Id:N}-{queue.AttemptCount}";
            var dispatchOperation = _operationResolver is null
                ? null
                : await _operationResolver.ResolveAsync(queue.AchTransaction, ct);

            if (dispatchOperation is not null
                && dispatchOperation.IsSupported
                && dispatchOperation.OperationKey == IntegrationGuaranteeConstants.ProcContrapartidas)
            {
                queue.QueueStatus = IncomingNachaDispatchQueueStatus.Dispatched;
                queue.ConfirmedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
                queue.NextAttemptAtUtc = null;
                queue.LastResponseCode = string.Empty;
                queue.LastErrorCode = string.Empty;
                queue.LastErrorMessage = string.Empty;
                AddAutomaticEvent(
                    queue,
                    "DispatchDelegatedToContrapartidas",
                    "Applied",
                    "Proc_Contrapartidas delegado al job de contrapartida en dry-run.",
                    IntegrationGuaranteeConstants.ProcContrapartidas);

                contrapartidaDispatchTargets.Add((queue.AchTransaction.AchCycleId, queue.AchTransaction.AchCycle.ClearingHouseId));
                confirmed++;
                continue;
            }

            var execution = new IncomingNachaIntegrationExecution
            {
                DispatchQueueId = queue.Id,
                EntryDetailId = classifications.TryGetValue(queue.IncomingNachaEntryClassificationId, out var executionClassification)
                    ? executionClassification.EntryDetailId
                    : null,
                AttemptNumber = queue.AttemptCount,
                ClearingHouseId = queue.ClearingHouseId,
                MethodName = SoapMethodName,
                SoapMethodName = SoapMethodName,
                ExecutionMode = NormalizeAuditValue(procTransaccionesRuntime.OperatingMode, 20),
                CorrelationId = correlationId,
                StartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
            };
            _context.IncomingNachaIntegrationExecution.Add(execution);

            try
            {
                if (!ingestions.TryGetValue(queue.IncomingNachaFileIngestionId, out var ingestion)
                    || !classifications.TryGetValue(queue.IncomingNachaEntryClassificationId, out var classification))
                {
                    queue.QueueStatus = IncomingNachaDispatchQueueStatus.Blocked;
                    queue.LastErrorCode = "MISSING_CONTEXT";
                    queue.LastErrorMessage = "No se encontró contexto de ingesta/clasificación.";
                    queue.NextAttemptAtUtc = null;
                    AddAutomaticEvent(queue, "IntegrationContextMissing", "Blocked", queue.LastErrorMessage, queue.LastErrorCode);
                    blocked++;
                    continue;
                }

                if (_localLivePreparationService is not null)
                {
                    var entry = await _context.EntryDetails
                        .SingleAsync(x => x.EntryDetailID == classification.EntryDetailId, ct);
                    await _localLivePreparationService.EnsureAsync(ingestion, entry, classification.FunctionalClass, ct);
                }

                var readinessContext = await EnsureProcTransaccionesReadinessAsync(queue.AchTransaction, ct);
                var resolution = await _mapper.ResolveAsync(queue, ingestion, classification, queue.AchTransaction, queue.AchTransaction.AchCycle, _timeProvider.GetLocalNow().DateTime, ct);
                EnsureSnapshotConsistency(readinessContext.Readiness, resolution);
                var requestXml = _mapper.BuildSoapBody(resolution.Contract);
                ApplyRequestAudit(execution, resolution, requestXml, procTransaccionesRuntime.Endpoint);
                await WriteMappingTraceAsync(
                    readinessContext.Operation,
                    resolution,
                    queue,
                    correlationId,
                    procTransaccionesRuntime.OperatingMode,
                    ct);

                var responseXml = await DispatchProcTransaccionesAsync(
                    requestXml,
                    queue,
                    procTransaccionesRuntime.OperatingMode,
                    ct);
                var parsed = _parser.Parse(responseXml);
                var finishedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

                var transportStatus = ResolveTransportStatus(parsed, procTransaccionesRuntime.OperatingMode);
                var catalogResult = transportStatus == IntegrationTransportStatus.Succeeded
                    ? await ResolveCoreResponseAsync(parsed.ResponseCode, finishedAtUtc, ct)
                    : null;
                var businessSuccess = transportStatus == IntegrationTransportStatus.Succeeded
                    && catalogResult is { IsKnownCode: true, BusinessStatus: IntegrationResponseBusinessStatus.Success };
                var retryAllowed = catalogResult is null ? parsed.IsRetryable : catalogResult.RetryAllowed;

                ApplyResponseAudit(
                    execution,
                    parsed,
                    responseXml,
                    finishedAtUtc,
                    transportStatus,
                    catalogResult,
                    retryAllowed,
                    procTransaccionesRuntime.OperatingMode);
                await ApplyAchResultAsync(execution, queue, parsed.ResponseCode, finishedAtUtc, transportStatus, ct);

                queue.LastResponseCode = parsed.ResponseCode;
                queue.LastErrorCode = businessSuccess ? string.Empty : parsed.ResponseCode;
                queue.LastErrorMessage = businessSuccess
                    ? string.Empty
                    : catalogResult?.Description ?? parsed.ResponseMessage;

                if (businessSuccess)
                {
                    if (catalogResult is not null)
                    {
                        ApplySuccessfulTransactionState(queue.AchTransaction, catalogResult, finishedAtUtc);
                    }
                    queue.QueueStatus = IncomingNachaDispatchQueueStatus.Confirmed;
                    queue.ConfirmedAtUtc = finishedAtUtc;
                    queue.NextAttemptAtUtc = null;
                    AddAutomaticEvent(queue, "IntegrationSucceeded", "Applied", "Integración confirmada exitosamente.");
                    confirmed++;
                }
                else if (transportStatus == IntegrationTransportStatus.Succeeded
                    && catalogResult is { RequiresManualReview: true })
                {
                    queue.QueueStatus = IncomingNachaDispatchQueueStatus.Blocked;
                    queue.NextAttemptAtUtc = null;
                    AddAutomaticEvent(
                        queue,
                        "IntegrationResponsePendingCatalog",
                        "Blocked",
                        "La respuesta del core requiere revisión manual o parametrización de catálogo.",
                        parsed.ResponseCode);
                    blocked++;
                }
                else if (retryAllowed)
                {
                    var normalizedCode = NormalizeTechnicalIntegrationCode(parsed.ResponseCode, parsed.ResponseMessage, integrationCodePolicies);
                    queue.LastErrorCode = normalizedCode;
                    queue.LastErrorMessage = parsed.ResponseMessage;

                    if (queue.AttemptCount < _resilienceOptions.MaxAttempts)
                    {
                        queue.QueueStatus = IncomingNachaDispatchQueueStatus.RetryPending;
                        queue.NextAttemptAtUtc = ComputeNextAttemptUtc(nowUtc, queue.AttemptCount);
                        AddAutomaticEvent(queue, "IntegrationRetryableFailed", "RetryPending", $"Falla técnica retryable. Reintento programado para {queue.NextAttemptAtUtc:O}.", normalizedCode);
                        retryPending++;
                    }
                    else
                    {
                        queue.QueueStatus = IncomingNachaDispatchQueueStatus.FailedFinal;
                        queue.NextAttemptAtUtc = null;
                        AddAutomaticEvent(queue, "MaxAttemptsExceeded", "FailedFinal", "Se agotó la política de reintentos para error técnico.", normalizedCode);
                        failedFinal++;
                    }
                }
                else
                {
                    queue.QueueStatus = IncomingNachaDispatchQueueStatus.FailedFinal;
                    queue.NextAttemptAtUtc = null;
                    queue.LastErrorCode = "IFUNC";
                    AddAutomaticEvent(queue, "IntegrationNonRetryableFailed", "FailedFinal", "Rechazo funcional no retryable.", queue.LastErrorCode);
                    failedFinal++;
                }
            }
            catch (InvalidOperationException ex)
            {
                queue.QueueStatus = IncomingNachaDispatchQueueStatus.Blocked;
                queue.LastErrorCode = ex.Message.StartsWith("PROC_TRANSACCIONES_DISABLED:", StringComparison.OrdinalIgnoreCase)
                    ? "PROC_TRANSACCIONES_DISABLED"
                    : ex.Message.StartsWith("PROC_TRANSACCIONES_READINESS_SERVICE_UNAVAILABLE:", StringComparison.OrdinalIgnoreCase)
                        ? "PROC_TRANSACCIONES_READINESS_SERVICE_UNAVAILABLE"
                    : ex.Message.StartsWith("MAPPING_SNAPSHOT_CHANGED:", StringComparison.OrdinalIgnoreCase)
                        ? "MAPPING_SNAPSHOT_CHANGED"
                        : "MAPPING_INVALID";
                queue.LastErrorMessage = ex.Message;
                execution.ResponseCode = queue.LastErrorCode;
                execution.ResponseMessage = ex.Message;
                execution.SoapResponseCode = queue.LastErrorCode;
                execution.SoapResponseDescription = ex.Message;
                execution.SoapTechnicalStatus = queue.LastErrorCode.Equals("PROC_TRANSACCIONES_DISABLED", StringComparison.OrdinalIgnoreCase)
                    ? TechnicalStatusDisabled
                    : queue.LastErrorCode.Equals("PROC_TRANSACCIONES_READINESS_SERVICE_UNAVAILABLE", StringComparison.OrdinalIgnoreCase)
                        ? TechnicalStatusTechnicalException
                    : queue.LastErrorCode.Equals("MAPPING_SNAPSHOT_CHANGED", StringComparison.OrdinalIgnoreCase)
                        ? TechnicalStatusTechnicalException
                    : TechnicalStatusTechnicalException;
                execution.IsSuccessful = false;
                execution.IsFunctionalRejection = false;
                execution.IsTechnicalFailure = true;
                execution.ProcessingStatus = IncomingNachaIndividualProcessingStatus.TechnicalFailed;
                execution.BusinessOutcome = IncomingNachaBusinessOutcome.NotProcessed;
                execution.ResultCode = string.Empty;
                execution.ResultDescription = string.Empty;
                execution.TechnicalErrorCode = queue.LastErrorCode;
                execution.TechnicalErrorMessage = ex.Message;
                execution.TransportStatus = IntegrationTransportStatus.Failed;
                execution.BusinessStatus = IntegrationResponseBusinessStatus.Unknown;
                execution.RetryAllowed = false;
                execution.RequiresManualReview = false;
                execution.ProcessedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
                execution.TechnicalException = BuildTechnicalException(ex);
                execution.RequestHash = execution.RequestHash == string.Empty ? Hash(ex.Message) : execution.RequestHash;
                execution.FinishedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
                execution.DurationMs = CalculateDurationMs(execution.StartedAtUtc, execution.FinishedAtUtc.Value);
                queue.NextAttemptAtUtc = null;
                AddAutomaticEvent(queue, "DispatchBlockedByMapping", "Blocked", ex.Message, queue.LastErrorCode);
                blocked++;
            }
            catch (Exception ex)
            {
                queue.LastErrorCode = NormalizeTechnicalIntegrationCode("TECHNICAL_ERROR", ex.Message, integrationCodePolicies);
                queue.LastErrorMessage = ex.Message;
                execution.ResponseCode = queue.LastErrorCode;
                execution.ResponseMessage = ex.Message;
                execution.FinishedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
                execution.IsSuccess = false;
                execution.IsRetryable = true;
                execution.SoapResponseCode = queue.LastErrorCode;
                execution.SoapResponseDescription = ex.Message;
                execution.SoapTechnicalStatus = TechnicalStatusTechnicalException;
                execution.IsSuccessful = false;
                execution.IsFunctionalRejection = false;
                execution.IsTechnicalFailure = true;
                execution.ProcessingStatus = queue.AttemptCount < _resilienceOptions.MaxAttempts
                    ? IncomingNachaIndividualProcessingStatus.RetryPending
                    : IncomingNachaIndividualProcessingStatus.TechnicalFailed;
                execution.BusinessOutcome = IncomingNachaBusinessOutcome.NotProcessed;
                execution.ResultCode = string.Empty;
                execution.ResultDescription = string.Empty;
                execution.TechnicalErrorCode = queue.LastErrorCode;
                execution.TechnicalErrorMessage = ex.Message;
                execution.TransportStatus = IntegrationTransportStatus.Failed;
                execution.BusinessStatus = IntegrationResponseBusinessStatus.Unknown;
                execution.RetryAllowed = true;
                execution.RequiresManualReview = false;
                execution.ProcessedAtUtc = execution.FinishedAtUtc;
                execution.TechnicalException = BuildTechnicalException(ex);
                execution.DurationMs = CalculateDurationMs(execution.StartedAtUtc, execution.FinishedAtUtc.Value);

                if (queue.AttemptCount < _resilienceOptions.MaxAttempts)
                {
                    queue.QueueStatus = IncomingNachaDispatchQueueStatus.RetryPending;
                    queue.NextAttemptAtUtc = ComputeNextAttemptUtc(nowUtc, queue.AttemptCount);
                    AddAutomaticEvent(queue, "IntegrationTechnicalFailed", "RetryPending", $"Excepción técnica. Reintento programado para {queue.NextAttemptAtUtc:O}.", queue.LastErrorCode);
                    retryPending++;
                }
                else
                {
                    queue.QueueStatus = IncomingNachaDispatchQueueStatus.FailedFinal;
                    queue.NextAttemptAtUtc = null;
                    AddAutomaticEvent(queue, "MaxAttemptsExceeded", "FailedFinal", "Excepción técnica con política de reintentos agotada.", queue.LastErrorCode);
                    failedFinal++;
                }
            }
        }

        if (_contrapartidaDispatchJobService is not null && contrapartidaDispatchTargets.Count > 0)
        {
            foreach (var target in contrapartidaDispatchTargets)
            {
                await _contrapartidaDispatchJobService.ProcessCycleAsync(
                    target.CycleId,
                    target.ClearingHouseId,
                    triggeredBy,
                    safeChunk,
                    ct);
            }
        }

        await _context.SaveChangesAsync(ct);
        var summary = $"Procesadas={queues.Count}, Confirmadas={confirmed}, Retry={retryPending}, FailedFinal={failedFinal}, Blocked={blocked}, WaitingWindow={waitingWindow}.";
        return new IncomingNachaPostProcessingRunResult(
            Planned: ids.Count,
            Picked: queues.Count,
            Confirmed: confirmed,
            RetryPending: retryPending,
            FailedFinal: failedFinal,
            Blocked: blocked,
            WaitingWindow: waitingWindow,
            Summary: summary);
    }

    private async Task<(int Released, int Blocked)> ReevaluateWaitingWindowAsync(
        DateTime nowUtc,
        DateTime nowLocal,
        CancellationToken ct)
    {
        var waitingQueues = await _context.IncomingNachaDispatchQueue
            .Include(x => x.AchTransaction)
            .ThenInclude(x => x.AchCycle)
            .Where(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.WaitingWindow)
            .ToListAsync(ct);

        var released = 0;
        var blocked = 0;
        var changed = false;

        foreach (var queue in waitingQueues)
        {
            var cycle = queue.AchTransaction?.AchCycle;
            var window = cycle is null
                ? new IncomingNachaDispatchWindowEvaluation(
                    IncomingNachaDispatchWindowPosition.Invalid,
                    null)
                : IncomingNachaDispatchWindowCalculator.Evaluate(
                    cycle,
                    nowLocal,
                    _timeProvider.LocalTimeZone);

            switch (window.Position)
            {
                case IncomingNachaDispatchWindowPosition.Open:
                    queue.QueueStatus = IncomingNachaDispatchQueueStatus.Queued;
                    queue.NextAttemptAtUtc = nowUtc;
                    queue.LastErrorCode = string.Empty;
                    queue.LastErrorMessage = string.Empty;
                    AddAutomaticEvent(
                        queue,
                        "DispatchWindowOpened",
                        "Applied",
                        "La ventana operativa fue reevaluada y está abierta; el despacho quedó en cola.");
                    released++;
                    changed = true;
                    break;

                case IncomingNachaDispatchWindowPosition.Before:
                    if (queue.NextAttemptAtUtc != window.NextEligibleAtUtc
                        || !string.IsNullOrEmpty(queue.LastErrorCode))
                    {
                        queue.NextAttemptAtUtc = window.NextEligibleAtUtc;
                        queue.LastErrorCode = string.Empty;
                        queue.LastErrorMessage = "En espera del inicio de la ventana operativa del ciclo.";
                        AddAutomaticEvent(
                            queue,
                            "DispatchWindowScheduled",
                            "Applied",
                            "Se calculó de forma determinística el próximo inicio de ventana operativa.");
                        changed = true;
                    }
                    break;

                case IncomingNachaDispatchWindowPosition.Expired:
                    queue.QueueStatus = IncomingNachaDispatchQueueStatus.Blocked;
                    queue.NextAttemptAtUtc = null;
                    queue.LastErrorCode = "WINDOW_EXPIRED";
                    queue.LastErrorMessage = "La ventana operativa del ciclo expiró; no se permite despacho automático.";
                    AddAutomaticEvent(
                        queue,
                        "DispatchWindowExpired",
                        "Blocked",
                        queue.LastErrorMessage,
                        queue.LastErrorCode);
                    blocked++;
                    changed = true;
                    break;

                default:
                    queue.QueueStatus = IncomingNachaDispatchQueueStatus.Blocked;
                    queue.NextAttemptAtUtc = null;
                    queue.LastErrorCode = "WINDOW_SCHEDULE_INVALID";
                    queue.LastErrorMessage = "No fue posible reevaluar de forma determinística la ventana operativa.";
                    AddAutomaticEvent(
                        queue,
                        "DispatchWindowInvalid",
                        "Blocked",
                        queue.LastErrorMessage,
                        queue.LastErrorCode);
                    blocked++;
                    changed = true;
                    break;
            }
        }

        if (changed)
        {
            await _context.SaveChangesAsync(ct);
        }

        return (released, blocked);
    }

    private static string Hash(string payload)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload ?? string.Empty));
        return Convert.ToHexString(bytes);
    }

    private DateTime ComputeNextAttemptUtc(DateTime nowUtc, int attemptCount)
    {
        var safeInitial = Math.Max(1, _resilienceOptions.InitialBackoffSeconds);
        var safeMultiplier = _resilienceOptions.BackoffMultiplier <= 1 ? 2d : _resilienceOptions.BackoffMultiplier;
        var exp = Math.Pow(safeMultiplier, Math.Max(0, attemptCount - 1));
        var seconds = Math.Min(_resilienceOptions.MaxBackoffSeconds, (int)Math.Round(safeInitial * exp, MidpointRounding.AwayFromZero));

        if (_resilienceOptions.EnableJitter)
        {
            seconds += Random.Shared.Next(0, Math.Max(1, _resilienceOptions.JitterMaxSeconds + 1));
        }

        return nowUtc.AddSeconds(seconds);
    }

    private static string NormalizeTechnicalIntegrationCode(
        string responseCode,
        string message,
        IReadOnlyDictionary<string, bool> integrationCodePolicies)
    {
        var normalizedCode = (responseCode ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedMessage = (message ?? string.Empty).ToUpperInvariant();

        var candidate = normalizedCode.Contains("503") ? "I503"
            : normalizedCode.Contains("500") ? "I500"
            : normalizedCode.Contains("TIMEOUT") || normalizedMessage.Contains("TIMEOUT") ? "ITIMEOUT"
            : normalizedCode.Contains("SOAP") || normalizedMessage.Contains("SOAP") ? "ISOAP"
            : "I500";

        if (integrationCodePolicies.Count == 0)
        {
            return candidate;
        }

        return integrationCodePolicies.ContainsKey(candidate) ? candidate : "I500";
    }

    private void AddAutomaticEvent(
        IncomingNachaDispatchQueue queue,
        string eventType,
        string eventStatus,
        string message,
        string? code = null)
    {
        _context.IncomingNachaProcessingEvents.Add(new IncomingNachaProcessingEvent
        {
            IncomingNachaFileIngestionId = queue.IncomingNachaFileIngestionId,
            AchTransactionId = queue.AchTransactionId,
            EventType = eventType,
            EventStatus = eventStatus,
            Message = string.IsNullOrWhiteSpace(code) ? message : $"{code}: {message}",
            EvidenceJson = JsonSerializer.Serialize(new
            {
                queueId = queue.Id,
                status = queue.QueueStatus,
                queue.AttemptCount,
                code = code ?? string.Empty
            }),
            OccurredAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            RaisedBy = "inbound.dispatch.orchestrator"
        });
    }

    private async Task<(TransactionIntegrationOperationResult Operation, IntegrationMappingReadinessResult Readiness)> EnsureProcTransaccionesReadinessAsync(AchTransaction transaction, CancellationToken ct)
    {
        if (_operationResolver is null || _mappingReadinessService is null)
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_READINESS_SERVICE_UNAVAILABLE: no se puede evaluar readiness de Proc_Transacciones sin resolver de operación o servicio de readiness.");
        }

        var operation = await _operationResolver.ResolveAsync(transaction, ct);
        if (operation is null)
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_READINESS_SERVICE_UNAVAILABLE: el resolvedor de operación devolvió null para Proc_Transacciones.");
        }
        if (!operation.IsSupported
            || operation.OperationKey != IntegrationGuaranteeConstants.ProcTransacciones
            || operation.MappingPurpose != IntegrationGuaranteeConstants.MonetaryCreditRequest)
        {
            throw new InvalidOperationException(
                $"INTEGRATION_OPERATION_MISMATCH: la transaccion {transaction.Id} no corresponde a Proc_Transacciones/MonetaryCreditRequest.");
        }

        var readiness = await _mappingReadinessService.EvaluateAsync(operation, ct);
        if (readiness is null)
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_READINESS_SERVICE_UNAVAILABLE: el servicio de readiness devolvió null para Proc_Transacciones.");
        }
        if (readiness.Status == "Failed" || !readiness.IsReady)
        {
            throw new InvalidOperationException(
                $"{readiness.Code}: no se puede construir payload Proc_Transacciones para transaccion {transaction.Id}; faltan mappings requeridos.");
        }

        if (!readiness.CanBuildPayload)
        {
            throw new InvalidOperationException(
                $"{readiness.Code}: readiness de Proc_Transacciones no permite construir payload para transaccion {transaction.Id}.");
        }

        if (readiness.UsesFallback)
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_REQUIRED_FIELD_USES_FALLBACK: Proc_Transacciones no puede marcar readiness Ok ni construir payload con fallback requerido.");
        }

        return (operation, readiness);
    }

    private static void EnsureSnapshotConsistency(
        IntegrationMappingReadinessResult readiness,
        ProcTransaccionesRequestResolution resolution)
    {
        if (!readiness.MappingSetId.HasValue
            || !readiness.MappingVersion.HasValue
            || string.IsNullOrWhiteSpace(readiness.MappingSnapshotHash))
        {
            throw new InvalidOperationException("MAPPING_SNAPSHOT_CHANGED: readiness no expuso identidad completa del mapping.");
        }

        if (readiness.MappingSetId.Value != resolution.MappingSetId
            || readiness.MappingVersion.Value != resolution.MappingVersion
            || !string.Equals(readiness.MappingSnapshotHash, resolution.MappingSnapshotHash, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("MAPPING_SNAPSHOT_CHANGED: el snapshot del mapping cambió entre readiness y dispatch.");
        }
    }

    private async Task<string> DispatchProcTransaccionesAsync(
        string requestXml,
        IncomingNachaDispatchQueue queue,
        string operatingMode,
        CancellationToken ct)
    {
        if (IsLiveMode(operatingMode))
        {
            return await _soapClient.ProcTransaccionesAsync(requestXml, ct);
        }

        if (IsDisabledMode(operatingMode))
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_DISABLED: Proc_Transacciones disabled; no se transmite externamente.");
        }

        const string code = "PROC_TRANSACCIONES_DRY_RUN";
        const string message = "Proc_Transacciones dry-run: payload generado, no transmitido.";

        AddAutomaticEvent(queue, "ProcTransaccionesDryRunGuardrail", "Blocked", message, code);

        return $"""
            <Envelope>
              <Body>
                <Proc_TransaccionesResponse>
                  <RTAACH>{code}</RTAACH>
                  <RTALOC>{message}</RTALOC>
                </Proc_TransaccionesResponse>
              </Body>
            </Envelope>
            """;
    }

    private void ApplyRequestAudit(
        IncomingNachaIntegrationExecution execution,
        ProcTransaccionesRequestResolution resolution,
        string requestXml,
        string soapEndpoint)
    {
        execution.MappingSetId = resolution.MappingSetId;
        execution.MappingVersion = resolution.MappingVersion;
        execution.MappingSnapshotHash = resolution.MappingSnapshotHash;
        execution.RequestPayloadXml = requestXml;
        execution.RequestHash = Hash(requestXml);
        execution.SoapEndpoint = NormalizeAuditValue(soapEndpoint, 500);
    }

    private void ApplyResponseAudit(
        IncomingNachaIntegrationExecution execution,
        ProcTransaccionesParsedResponse parsed,
        string responseXml,
        DateTime finishedAtUtc,
        IntegrationTransportStatus transportStatus,
        IntegrationResponseCatalogResult? catalogResult,
        bool retryAllowed,
        string operatingMode)
    {
        var soapCode = NormalizeAuditValue(parsed.ResponseCode, 80);
        var soapDescription = NormalizeAuditValue(catalogResult?.Description ?? parsed.ResponseMessage, 4000);
        var businessStatus = catalogResult?.BusinessStatus ?? IntegrationResponseBusinessStatus.Unknown;
        var isSuccessful = transportStatus == IntegrationTransportStatus.Succeeded
            && catalogResult is { IsKnownCode: true, BusinessStatus: IntegrationResponseBusinessStatus.Success };
        var isFunctionalRejection = businessStatus == IntegrationResponseBusinessStatus.Rejected;
        var isTechnicalFailure = transportStatus is IntegrationTransportStatus.Failed or IntegrationTransportStatus.TimedOut;

        execution.ResponsePayloadXml = responseXml;
        execution.ResponseHash = Hash(responseXml);
        execution.SoapResponseCode = soapCode;
        execution.SoapResponseDescription = soapDescription;
        execution.SoapTechnicalStatus = transportStatus == IntegrationTransportStatus.Succeeded
            ? TechnicalStatusSucceeded
            : ResolveSoapTechnicalStatus(parsed, isSuccessful, isFunctionalRejection, operatingMode);
        execution.ResponseCatalogId = catalogResult?.CatalogId;
        execution.TransportStatus = transportStatus;
        execution.BusinessStatus = businessStatus;
        execution.RetryAllowed = retryAllowed;
        execution.RequiresManualReview = catalogResult?.RequiresManualReview ?? false;
        execution.ProcessedAtUtc = finishedAtUtc;
        execution.IsSuccessful = isSuccessful;
        execution.IsFunctionalRejection = isFunctionalRejection;
        execution.IsTechnicalFailure = isTechnicalFailure;
        execution.ResponseCode = soapCode;
        execution.ResponseMessage = soapDescription;
        execution.IsSuccess = isSuccessful;
        execution.IsRetryable = retryAllowed;
        execution.FinishedAtUtc = finishedAtUtc;
        execution.DurationMs = CalculateDurationMs(execution.StartedAtUtc, finishedAtUtc);
    }

    private async Task ApplyAchResultAsync(
        IncomingNachaIntegrationExecution execution,
        IncomingNachaDispatchQueue queue,
        string responseCode,
        DateTime processedAtUtc,
        IntegrationTransportStatus transportStatus,
        CancellationToken ct)
    {
        if (transportStatus != IntegrationTransportStatus.Succeeded)
        {
            execution.ProcessingStatus = IncomingNachaIndividualProcessingStatus.TechnicalFailed;
            execution.BusinessOutcome = IncomingNachaBusinessOutcome.NotProcessed;
            execution.ResultCode = string.Empty;
            execution.ResultDescription = string.Empty;
            return;
        }

        var resolution = _achResultResolver is null
            ? null
            : await _achResultResolver.ResolveAsync(new IncomingNachaAchResultRequest(
                queue.ClearingHouseId,
                responseCode,
                queue.AchTransaction.Type == TransactionTypeEnum.Return ? AchReturnFlowType.Return : AchReturnFlowType.Any,
                IsDebit: queue.AchTransaction.Type == TransactionTypeEnum.Debit,
                IsCredit: queue.AchTransaction.Type == TransactionTypeEnum.Credit,
                IsPrenotification: queue.AchTransaction.Type == TransactionTypeEnum.Prenotification,
                IsReturn: queue.AchTransaction.Type == TransactionTypeEnum.Return,
                processedAtUtc), ct);

        execution.ProcessingStatus = IncomingNachaIndividualProcessingStatus.Completed;
        execution.AchReturnCodeId = resolution?.AchReturnCodeId;
        execution.ResultCode = NormalizeAuditValue(responseCode, 20);
        execution.ResultDescription = NormalizeAuditValue(
            resolution is { IsResolved: true }
                ? resolution.ResultDescription
                : execution.SoapResponseDescription,
            500);
        execution.BusinessOutcome = resolution is { IsResolved: true }
            ? resolution.BusinessOutcome
            : execution.BusinessStatus switch
            {
                IntegrationResponseBusinessStatus.Success => IncomingNachaBusinessOutcome.Successful,
                IntegrationResponseBusinessStatus.Rejected => IncomingNachaBusinessOutcome.Rejected,
                _ => IncomingNachaBusinessOutcome.PendingResponse
            };
    }

    private string ResolveSoapTechnicalStatus(
        ProcTransaccionesParsedResponse parsed,
        bool isSuccessful,
        bool isFunctionalRejection,
        string operatingMode)
    {
        if (IsDisabledMode(operatingMode))
        {
            return TechnicalStatusDisabled;
        }

        if (IsDryRunLikeMode(operatingMode))
        {
            return TechnicalStatusDryRun;
        }

        if (isSuccessful)
        {
            return TechnicalStatusSucceeded;
        }

        if (parsed.ResponseCode.Equals("SOAP_FAULT", StringComparison.OrdinalIgnoreCase)
            || parsed.RawResponse.Contains("<Fault", StringComparison.OrdinalIgnoreCase)
            || parsed.RawResponse.Contains(":Fault", StringComparison.OrdinalIgnoreCase))
        {
            return TechnicalStatusSoapFault;
        }

        if (parsed.ResponseCode.Equals("PARSER_ERROR", StringComparison.OrdinalIgnoreCase))
        {
            return TechnicalStatusParserError;
        }

        if (isFunctionalRejection)
        {
            return TechnicalStatusFunctionalRejection;
        }

        if (parsed.IsRetryable)
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
        ProcTransaccionesParsedResponse parsed,
        string operatingMode)
    {
        if (IsDisabledMode(operatingMode) || IsDryRunLikeMode(operatingMode))
        {
            return IntegrationTransportStatus.NotExecuted;
        }

        if (parsed.ResponseCode.Contains("TIMEOUT", StringComparison.OrdinalIgnoreCase)
            || parsed.ResponseMessage.Contains("TIMEOUT", StringComparison.OrdinalIgnoreCase))
        {
            return IntegrationTransportStatus.TimedOut;
        }

        if (parsed.ResponseCode.Equals("SOAP_FAULT", StringComparison.OrdinalIgnoreCase)
            || parsed.ResponseCode.Equals("PARSER_ERROR", StringComparison.OrdinalIgnoreCase)
            || parsed.ResponseCode.Equals("EMPTY", StringComparison.OrdinalIgnoreCase)
            || parsed.RawResponse.Contains("<Fault", StringComparison.OrdinalIgnoreCase)
            || parsed.RawResponse.Contains(":Fault", StringComparison.OrdinalIgnoreCase))
        {
            return IntegrationTransportStatus.Failed;
        }

        return IntegrationTransportStatus.Succeeded;
    }

    private void ApplySuccessfulTransactionState(
        AchTransaction transaction,
        IntegrationResponseCatalogResult catalog,
        DateTime processedAtUtc)
    {
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
            PayloadJson = JsonSerializer.Serialize(new
            {
                responseCatalogId = catalog.CatalogId,
                category = catalog.Category,
                method = catalog.Method,
                businessStatus = catalog.BusinessStatus.ToString()
            })
        });
    }

    private async Task<(string Endpoint, string OperatingMode)> ResolveProcTransaccionesRuntimeAsync(
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

    private async Task WriteMappingTraceAsync(
        TransactionIntegrationOperationResult? operation,
        ProcTransaccionesRequestResolution resolution,
        IncomingNachaDispatchQueue queue,
        string correlationId,
        string operatingMode,
        CancellationToken ct)
    {
        if (_mappingTraceWriter is null || operation is null)
        {
            return;
        }

        await _mappingTraceWriter.WriteAsync(
            operation,
            resolution.Contract,
            queue.AchTransactionId,
            queue.AchTransaction.Reference,
            correlationId,
            dryRun: !IsLiveMode(operatingMode),
            externalTransmission: IsLiveMode(operatingMode),
            ct);
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
}
