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
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaPostProcessingOrchestrator : IIncomingNachaPostProcessingOrchestrator
{
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
        ISoapIntegrationSettingsService? soapIntegrationSettingsService = null)
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
    }

    public async Task<IncomingNachaPostProcessingRunResult> ExecuteAsync(
        int chunkSize,
        string triggeredBy,
        CancellationToken ct = default)
    {
        var safeChunk = Math.Clamp(chunkSize, 10, 500);
        var nowUtc = DateTime.UtcNow;
        var integrationCodePolicies = await _context.AchFileRejectionCodes
            .AsNoTracking()
            .Where(x => x.IsActive && x.AppliesToStage == "Integration")
            .ToDictionaryAsync(x => x.Code.ToUpper(), x => x.IsRetryable, ct);

        var releasedFromWaitingWindow = await _context.IncomingNachaDispatchQueue
            .Where(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.WaitingWindow)
            .Where(x => x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= nowUtc)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.QueueStatus, IncomingNachaDispatchQueueStatus.Queued)
                .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow), ct);

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
            return new IncomingNachaPostProcessingRunResult(0, 0, 0, 0, 0, 0, 0, "Sin elementos en cola.");
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
        var blocked = 0;
        var waitingWindow = releasedFromWaitingWindow;
        var contrapartidaDispatchTargets = new HashSet<(string CycleId, int ClearingHouseId)>();

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
                queue.ConfirmedAtUtc = DateTime.UtcNow;
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
                MethodName = SoapMethodName,
                SoapMethodName = SoapMethodName,
                ExecutionMode = NormalizeAuditValue(_dispatchOptions.NormalizedMode, 20),
                CorrelationId = correlationId,
                StartedAtUtc = DateTime.UtcNow
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

                var readinessContext = await EnsureProcTransaccionesReadinessAsync(queue.AchTransaction, ct);
                var resolution = await _mapper.ResolveAsync(queue, ingestion, classification, queue.AchTransaction, queue.AchTransaction.AchCycle, DateTime.Now, ct);
                EnsureSnapshotConsistency(readinessContext.Readiness, resolution);
                var requestXml = _mapper.BuildSoapBody(resolution.Contract);
                var dispatchEndpoint = await ResolveProcTransaccionesEndpointAsync(ct);
                ApplyRequestAudit(execution, resolution, requestXml, dispatchEndpoint);
                await WriteMappingTraceAsync(readinessContext.Operation, resolution, queue, correlationId, ct);

                var responseXml = await DispatchProcTransaccionesAsync(requestXml, queue, ct);
                var parsed = _parser.Parse(responseXml);
                var finishedAtUtc = DateTime.UtcNow;

                ApplyResponseAudit(execution, parsed, responseXml, finishedAtUtc);

                queue.LastResponseCode = parsed.ResponseCode;
                queue.LastErrorCode = parsed.IsSuccess ? string.Empty : parsed.ResponseCode;
                queue.LastErrorMessage = parsed.IsSuccess ? string.Empty : parsed.ResponseMessage;

                if (parsed.IsSuccess || parsed.IsPartialSuccess)
                {
                    queue.QueueStatus = IncomingNachaDispatchQueueStatus.Confirmed;
                    queue.ConfirmedAtUtc = DateTime.UtcNow;
                    queue.NextAttemptAtUtc = null;
                    AddAutomaticEvent(queue, "IntegrationSucceeded", "Applied", "Integración confirmada exitosamente.");
                    confirmed++;
                }
                else if (parsed.IsRetryable)
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
                execution.TechnicalException = BuildTechnicalException(ex);
                execution.RequestHash = execution.RequestHash == string.Empty ? Hash(ex.Message) : execution.RequestHash;
                execution.FinishedAtUtc = DateTime.UtcNow;
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
                execution.FinishedAtUtc = DateTime.UtcNow;
                execution.IsSuccess = false;
                execution.IsRetryable = true;
                execution.SoapResponseCode = queue.LastErrorCode;
                execution.SoapResponseDescription = ex.Message;
                execution.SoapTechnicalStatus = TechnicalStatusTechnicalException;
                execution.IsSuccessful = false;
                execution.IsFunctionalRejection = false;
                execution.IsTechnicalFailure = true;
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
            OccurredAtUtc = DateTime.UtcNow,
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

    private async Task<string> DispatchProcTransaccionesAsync(string requestXml, IncomingNachaDispatchQueue queue, CancellationToken ct)
    {
        if (_dispatchOptions.IsLive)
        {
            return await _soapClient.ProcTransaccionesAsync(requestXml, ct);
        }

        if (_dispatchOptions.IsDisabled)
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
        DateTime finishedAtUtc)
    {
        var soapCode = NormalizeAuditValue(parsed.ResponseCode, 80);
        var soapDescription = NormalizeAuditValue(parsed.ResponseMessage, 4000);
        var isSuccessful = parsed.IsSuccess || parsed.IsPartialSuccess;
        var isFunctionalRejection = !_dispatchOptions.IsDryRunLike
            && !_dispatchOptions.IsDisabled
            && !isSuccessful
            && parsed.IsFunctionalRejection
            && !IsTechnicalAnomalyCode(parsed.ResponseCode);
        var isTechnicalFailure = !_dispatchOptions.IsDryRunLike
            && !_dispatchOptions.IsDisabled
            && !isSuccessful
            && !isFunctionalRejection;

        execution.ResponsePayloadXml = responseXml;
        execution.ResponseHash = Hash(responseXml);
        execution.SoapResponseCode = soapCode;
        execution.SoapResponseDescription = soapDescription;
        execution.SoapTechnicalStatus = ResolveSoapTechnicalStatus(parsed, isSuccessful, isFunctionalRejection);
        execution.IsSuccessful = isSuccessful;
        execution.IsFunctionalRejection = isFunctionalRejection;
        execution.IsTechnicalFailure = isTechnicalFailure;
        execution.ResponseCode = soapCode;
        execution.ResponseMessage = soapDescription;
        execution.IsSuccess = parsed.IsSuccess;
        execution.IsRetryable = parsed.IsRetryable;
        execution.FinishedAtUtc = finishedAtUtc;
        execution.DurationMs = CalculateDurationMs(execution.StartedAtUtc, finishedAtUtc);
    }

    private string ResolveSoapTechnicalStatus(
        ProcTransaccionesParsedResponse parsed,
        bool isSuccessful,
        bool isFunctionalRejection)
    {
        if (_dispatchOptions.IsDisabled)
        {
            return TechnicalStatusDisabled;
        }

        if (_dispatchOptions.IsDryRunLike)
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

    private async Task<string> ResolveProcTransaccionesEndpointAsync(CancellationToken ct)
    {
        if (_soapIntegrationSettingsService is null)
        {
            return string.Empty;
        }

        var settings = await _soapIntegrationSettingsService.GetAsync(ct);
        return settings.WscfaachMappings
            .FirstOrDefault(x => string.Equals(x.MethodName, SoapMethodName, StringComparison.OrdinalIgnoreCase))
            ?.Endpoint
            ?.Trim() ?? string.Empty;
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
            dryRun: !_dispatchOptions.IsLive,
            externalTransmission: false,
            ct);
    }
}
