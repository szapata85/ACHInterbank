using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaPostProcessingOrchestrator : IIncomingNachaPostProcessingOrchestrator
{
    private readonly AchDbContext _context;
    private readonly IProcTransaccionesRequestMapper _mapper;
    private readonly IProcTransaccionesResponseParser _parser;
    private readonly IWscfaachSoapClient _soapClient;
    private readonly IncomingNachaDispatchResilienceOptions _resilienceOptions;
    private readonly ITransactionIntegrationOperationResolver? _operationResolver;
    private readonly IIntegrationMappingReadinessService? _mappingReadinessService;

    public IncomingNachaPostProcessingOrchestrator(
        AchDbContext context,
        IProcTransaccionesRequestMapper mapper,
        IProcTransaccionesResponseParser parser,
        IWscfaachSoapClient soapClient,
        IOptions<IncomingNachaDispatchResilienceOptions>? resilienceOptions = null,
        ITransactionIntegrationOperationResolver? operationResolver = null,
        IIntegrationMappingReadinessService? mappingReadinessService = null)
    {
        _context = context;
        _mapper = mapper;
        _parser = parser;
        _soapClient = soapClient;
        _resilienceOptions = resilienceOptions?.Value ?? new IncomingNachaDispatchResilienceOptions();
        _operationResolver = operationResolver;
        _mappingReadinessService = mappingReadinessService;
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

        foreach (var queue in queues)
        {
            queue.QueueStatus = IncomingNachaDispatchQueueStatus.Dispatching;
            queue.LastAttemptAtUtc = nowUtc;
            queue.AttemptCount += 1;
            AddAutomaticEvent(queue, "DispatchStarted", "Applied", "Dispatch automático iniciado.");

            var correlationId = $"in-nacha-{queue.Id:N}-{queue.AttemptCount}";
            var execution = new IncomingNachaIntegrationExecution
            {
                DispatchQueueId = queue.Id,
                MethodName = "Proc_Transacciones",
                CorrelationId = correlationId,
                StartedAtUtc = nowUtc
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

                await EnsureProcTransaccionesReadinessAsync(queue.AchTransaction, ct);
                var resolution = await _mapper.ResolveAsync(queue, ingestion, classification, queue.AchTransaction, queue.AchTransaction.AchCycle, DateTime.Now, ct);
                var requestXml = _mapper.BuildSoapBody(resolution.Contract);
                var responseXml = await _soapClient.ProcTransaccionesAsync(requestXml, ct);
                var parsed = _parser.Parse(responseXml);

                execution.MappingSetId = resolution.MappingSetId;
                execution.MappingVersion = resolution.MappingVersion;
                execution.MappingSnapshotHash = resolution.MappingSnapshotHash;
                execution.RequestPayloadXml = requestXml;
                execution.ResponsePayloadXml = responseXml;
                execution.RequestHash = Hash(requestXml);
                execution.ResponseHash = Hash(responseXml);
                execution.ResponseCode = parsed.ResponseCode;
                execution.ResponseMessage = parsed.ResponseMessage;
                execution.IsSuccess = parsed.IsSuccess;
                execution.IsRetryable = parsed.IsRetryable;
                execution.FinishedAtUtc = DateTime.UtcNow;

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
                queue.LastErrorCode = "MAPPING_INVALID";
                queue.LastErrorMessage = ex.Message;
                execution.ResponseCode = "MAPPING_INVALID";
                execution.ResponseMessage = ex.Message;
                execution.RequestHash = execution.RequestHash == string.Empty ? Hash(ex.Message) : execution.RequestHash;
                execution.FinishedAtUtc = DateTime.UtcNow;
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

    private async Task EnsureProcTransaccionesReadinessAsync(AchTransaction transaction, CancellationToken ct)
    {
        if (_operationResolver is null || _mappingReadinessService is null)
        {
            return;
        }

        var operation = await _operationResolver.ResolveAsync(transaction, ct);
        if (!operation.IsSupported
            || operation.OperationKey != IntegrationGuaranteeConstants.ProcTransacciones
            || operation.MappingPurpose != IntegrationGuaranteeConstants.MonetaryCreditRequest)
        {
            throw new InvalidOperationException(
                $"INTEGRATION_OPERATION_MISMATCH: la transaccion {transaction.Id} no corresponde a Proc_Transacciones/MonetaryCreditRequest.");
        }

        var readiness = await _mappingReadinessService.EvaluateAsync(operation, ct);
        if (!readiness.IsReady)
        {
            throw new InvalidOperationException(
                $"{readiness.Code}: no se puede construir payload Proc_Transacciones para transaccion {transaction.Id}; faltan mappings requeridos.");
        }
    }
}
