using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Validation;
using Cfa.ACHInterbank.Application.ACH.Responses.Repositories;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using System.Text.Json;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Notification.Services;

public sealed class NotificarRespuestaAchUseCase : INotificarRespuestaAchUseCase
{
    private readonly IAchResponseRepository _responseRepository;
    private readonly IAchResponseNotificationAttemptRepository _attemptRepository;
    private readonly IRegistrarRespuestaAchCommandMapper _mapper;
    private readonly IRespuestaTransaccionesAchGateway _gateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionIntegrationOperationResolver? _operationResolver;
    private readonly IIntegrationMappingReadinessService? _mappingReadinessService;
    private readonly IIntegrationMappingTraceWriter? _mappingTraceWriter;
    private readonly IAxonNotificationClaimStore? _claimStore;

    public NotificarRespuestaAchUseCase(
        IAchResponseRepository responseRepository,
        IAchResponseNotificationAttemptRepository attemptRepository,
        IRegistrarRespuestaAchCommandMapper mapper,
        IRespuestaTransaccionesAchGateway gateway,
        IUnitOfWork unitOfWork,
        ITransactionIntegrationOperationResolver? operationResolver = null,
        IIntegrationMappingReadinessService? mappingReadinessService = null,
        IIntegrationMappingTraceWriter? mappingTraceWriter = null,
        IAxonNotificationClaimStore? claimStore = null)
    {
        _responseRepository = responseRepository;
        _attemptRepository = attemptRepository;
        _mapper = mapper;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _operationResolver = operationResolver;
        _mappingReadinessService = mappingReadinessService;
        _mappingTraceWriter = mappingTraceWriter;
        _claimStore = claimStore;
    }

    public async Task<NotificarRespuestaAchResult> ExecuteAsync(NotificarRespuestaAchCommand command, CancellationToken cancellationToken = default)
    {
        if (command.NotificationAttemptId <= 0)
            return new(false, false, false, false, false, null, null, null, null, null, "NotificationAttemptId inválido");

        if (_claimStore is null)
            return new(false, false, false, false, true, null, null,
                "AXON_CLAIM_STORE_UNAVAILABLE", null, null,
                "No existe coordinador persistente de notificaciones: transporte prohibido.");

        if (_claimStore is not null)
            await _claimStore.QuarantineExpiredAsync(DateTime.UtcNow, cancellationToken);
        var attempt = await _attemptRepository.FindByIdAsync(command.NotificationAttemptId, cancellationToken);
        if (attempt is null)
            return new(false, false, false, false, false, null, null, null, null, null, "Intento no encontrado");

        if (attempt.EstadoNotificacion is not AchResponseNotificationStatus.Pendiente and not AchResponseNotificationStatus.PendienteReintento)
            return new(true, true, true, attempt.ExisteError ?? false, false, attempt.EstadoNotificacion, attempt.AchResponse?.EstadoProcesamiento, attempt.CodigoError, attempt.DescripcionError, null, "Intento ya procesado");

        var response = attempt.AchResponse;
        if (response is null)
            return new(false, false, false, false, false, null, null, null, null, null, "Respuesta ACH asociada no encontrada");

        var readinessError = await ValidateDifferentialResponseReadinessAsync(response.IdTransaccion, cancellationToken);
        if (readinessError is not null)
        {
            attempt.ExisteError = true;
            attempt.CodigoError = readinessError.Value.Code;
            attempt.DescripcionError = readinessError.Value.Message;
            attempt.FechaEnvio = DateTime.UtcNow;
            attempt.EstadoNotificacion = AchResponseNotificationStatus.ErrorFuncional;
            response.EstadoProcesamiento = AchResponseProcessingStatus.ErrorFuncional;
            response.FechaActualizacion = DateTime.UtcNow;
            if (!await _claimStore.TryFinishPreflightAsync(attempt.Id, response.Id,
                new AxonNotificationCompletion(attempt.EstadoNotificacion, response.EstadoProcesamiento,
                    true, attempt.CodigoError, attempt.DescripcionError, null, null, null),
                DateTime.UtcNow, cancellationToken))
                return new(true, true, true, false, true, AchResponseNotificationStatus.RequiereRevisionManual,
                    AchResponseProcessingStatus.RequiereRevisionManual, "AXON_PREFLIGHT_FENCED", null, null, null);

            return new(true, true, false, true, false, attempt.EstadoNotificacion, response.EstadoProcesamiento, readinessError.Value.Code, readinessError.Value.Message, null, null);
        }

        var cmd = _mapper.Map(response, attempt);
        var validationErrors = new RegistrarRespuestaAchCommandValidator().Validate(cmd);
        if (validationErrors.Count > 0)
        {
            attempt.ExisteError = true;
            attempt.CodigoError = "REGISTRAR_RESPUESTA_COMMAND_INVALID";
            attempt.DescripcionError = string.Join("; ", validationErrors);
            attempt.EstadoNotificacion = AchResponseNotificationStatus.ErrorFuncional;
            response.EstadoProcesamiento = AchResponseProcessingStatus.ErrorFuncional;
            response.FechaActualizacion = DateTime.UtcNow;
            if (!await _claimStore.TryFinishPreflightAsync(attempt.Id, response.Id,
                new AxonNotificationCompletion(attempt.EstadoNotificacion, response.EstadoProcesamiento,
                    true, attempt.CodigoError, attempt.DescripcionError, null, null, null),
                DateTime.UtcNow, cancellationToken))
                return new(true, true, true, false, true, AchResponseNotificationStatus.RequiereRevisionManual,
                    AchResponseProcessingStatus.RequiereRevisionManual, "AXON_PREFLIGHT_FENCED", null, null, null);

            return new(true, true, false, true, false, attempt.EstadoNotificacion,
                response.EstadoProcesamiento, attempt.CodigoError, attempt.DescripcionError, null, null);
        }

        attempt.RequestPayload = JsonSerializer.Serialize(new
        {
            idCanal = cmd.IdCanal,
            nombreCanal = cmd.NombreCanal,
            idTransaccion = cmd.IdTransaccion,
            idEstado = cmd.IdEstado,
            causal = cmd.Causal,
            idTransaccionAxon = cmd.IdTransaccionServicioExterno,
            descripcionCausal = cmd.DescripcionCausal
        });

        // No automatic replay after the transport boundary. The token is scoped to
        // this attempt and is required again for atomic completion.
        var gatewayInvoked = false;
        var transportIntentMarked = false;
        string? claimToken = null;
        try
        {
            var traceError = await WriteDifferentialResponseTraceAsync(cmd, response.IdTransaccion, command.CorrelationId ?? response.CorrelationId, cancellationToken);
            if (traceError is not null)
            {
                attempt.ExisteError = true;
                attempt.CodigoError = traceError.Value.Code;
                attempt.DescripcionError = traceError.Value.Message;
                attempt.FechaEnvio = DateTime.UtcNow;
                attempt.EstadoNotificacion = AchResponseNotificationStatus.ErrorFuncional;
                response.EstadoProcesamiento = AchResponseProcessingStatus.ErrorFuncional;
                response.FechaActualizacion = DateTime.UtcNow;
                if (!await _claimStore.TryFinishPreflightAsync(attempt.Id, response.Id,
                    new AxonNotificationCompletion(attempt.EstadoNotificacion, response.EstadoProcesamiento,
                        true, attempt.CodigoError, attempt.DescripcionError, null, null, null),
                    DateTime.UtcNow, cancellationToken))
                    return new(true, true, true, false, true, AchResponseNotificationStatus.RequiereRevisionManual,
                        AchResponseProcessingStatus.RequiereRevisionManual, "AXON_PREFLIGHT_FENCED", null, null, null);

                return new(true, true, false, true, false, attempt.EstadoNotificacion, response.EstadoProcesamiento, traceError.Value.Code, traceError.Value.Message, null, null);
            }

            if (_claimStore is not null)
            {
                claimToken = Guid.NewGuid().ToString("N");
                var claimAtUtc = DateTime.UtcNow;
                if (!await _claimStore.TryClaimAsync(attempt.Id, claimToken, claimAtUtc, cancellationToken))
                    return new(true, true, true, false, false, attempt.EstadoNotificacion,
                        response.EstadoProcesamiento, "AXON_CLAIM_ALREADY_TAKEN", "El intento pertenece a otro worker.", null, null);
                attempt.EstadoNotificacion = AchResponseNotificationStatus.InFlight;
                attempt.ClaimToken = claimToken;
                attempt.LeaseExpiresAtUtc = claimAtUtc.AddMinutes(30);
                if (!await _claimStore.MarkTransportIntentAsync(attempt.Id, claimToken, DateTime.UtcNow, cancellationToken))
                    throw new InvalidOperationException("AXON_CLAIM_FENCE_FAILED: no invocar SOAP sin lease vigente.");
                attempt.TransportStartedAtUtc = DateTime.UtcNow;
                transportIntentMarked = true;
            }
            gatewayInvoked = true;
            var result = await _gateway.RegistrarRespuestaAsync(cmd, cancellationToken);
            attempt.ResponsePayload = JsonSerializer.Serialize(new
            {
                existeError = result.ExisteError,
                codigoError = result.CodigoError,
                descripcionError = result.DescripcionError
            });
            attempt.ExisteError = result.ExisteError;
            attempt.CodigoError = result.CodigoError;
            attempt.DescripcionError = result.DescripcionError;
            attempt.FechaEnvio = DateTime.UtcNow;

            if (result.ExisteError)
            {
                attempt.EstadoNotificacion = AchResponseNotificationStatus.ErrorFuncional;
                response.EstadoProcesamiento = AchResponseProcessingStatus.ErrorFuncional;
            }
            else
            {
                attempt.EstadoNotificacion = AchResponseNotificationStatus.Exitosa;
                response.EstadoProcesamiento = AchResponseProcessingStatus.Notificada;
            }

            response.FechaActualizacion = DateTime.UtcNow;
            var completed = await _claimStore.TryFinalizeAsync(attempt.Id, response.Id, claimToken!,
                new AxonNotificationCompletion(attempt.EstadoNotificacion, response.EstadoProcesamiento,
                    result.ExisteError, result.CodigoError, result.DescripcionError, null,
                    attempt.RequestPayload, attempt.ResponsePayload), DateTime.UtcNow, CancellationToken.None);
            if (!completed)
            {
                await _claimStore.RecordLateEvidenceAsync(attempt.Id, response.Id, claimToken!,
                    "AXON_LATE_RESULT_FENCED", attempt.ResponsePayload, CancellationToken.None);
                return new(true, true, false, false, true, AchResponseNotificationStatus.RequiereRevisionManual,
                    AchResponseProcessingStatus.RequiereRevisionManual, "AXON_LATE_RESULT_FENCED",
                    "Resultado tardio archivado, sin modificar estado.", null, null);
            }
            return new(true, true, false, result.ExisteError, false, attempt.EstadoNotificacion, response.EstadoProcesamiento, result.CodigoError, result.DescripcionError, null, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (gatewayInvoked)
            {
                // Persist with an independent token: the original caller has cancelled.
                attempt.EstadoNotificacion = AchResponseNotificationStatus.RequiereRevisionManual;
                attempt.CodigoError = "AXON_CANCELLED_OUTCOME_UNKNOWN";
                attempt.DescripcionError = "Cancelacion despues de iniciar transporte: resultado requiere conciliacion.";
                attempt.FechaEnvio = DateTime.UtcNow;
                response.EstadoProcesamiento = AchResponseProcessingStatus.RequiereRevisionManual;
                response.FechaActualizacion = DateTime.UtcNow;
                var completed = await _claimStore.TryFinalizeAsync(attempt.Id, response.Id, claimToken!,
                    new AxonNotificationCompletion(attempt.EstadoNotificacion, response.EstadoProcesamiento,
                        true, attempt.CodigoError, attempt.DescripcionError, null,
                        attempt.RequestPayload, attempt.ResponsePayload), DateTime.UtcNow, CancellationToken.None);
                if (!completed)
                    await _claimStore.RecordLateEvidenceAsync(attempt.Id, response.Id, claimToken!,
                        "AXON_CANCELLED_LATE_FENCED", null, CancellationToken.None);
            }
            throw;
        }
        catch (Exception ex)
        {
            var deliveryOutcomeUnknown = gatewayInvoked || transportIntentMarked
                || ex is TimeoutException or TaskCanceledException;
            attempt.ResponsePayload = JsonSerializer.Serialize(new
            {
                technicalStatus = deliveryOutcomeUnknown ? "ResultadoDesconocido" : "ErrorTecnico",
                exceptionType = ex.GetType().Name
            });
            attempt.EstadoNotificacion = deliveryOutcomeUnknown
                ? AchResponseNotificationStatus.RequiereRevisionManual
                : AchResponseNotificationStatus.ErrorTecnico;
            attempt.ErrorTecnico = ex.Message;
            attempt.FechaEnvio = DateTime.UtcNow;
            response.EstadoProcesamiento = deliveryOutcomeUnknown
                ? AchResponseProcessingStatus.RequiereRevisionManual
                : AchResponseProcessingStatus.PendienteReintento;
            response.FechaActualizacion = DateTime.UtcNow;
            if (claimToken is not null)
            {
                var completed = await _claimStore.TryFinalizeAsync(attempt.Id, response.Id, claimToken,
                    new AxonNotificationCompletion(attempt.EstadoNotificacion, response.EstadoProcesamiento,
                        true, deliveryOutcomeUnknown ? "AXON_OUTCOME_UNKNOWN_RECONCILE" : "AXON_PRETRANSPORT_TECHNICAL",
                        "Error tecnico; consultar la evidencia antes de reintentar.", ex.GetType().Name,
                        attempt.RequestPayload, attempt.ResponsePayload), DateTime.UtcNow, CancellationToken.None);
                if (!completed)
                {
                    await _claimStore.RecordLateEvidenceAsync(attempt.Id, response.Id, claimToken,
                        "AXON_EXCEPTION_LATE_FENCED", attempt.ResponsePayload, CancellationToken.None);
                    return new(true, true, false, false, true, AchResponseNotificationStatus.RequiereRevisionManual,
                        AchResponseProcessingStatus.RequiereRevisionManual, "AXON_LATE_RESULT_FENCED", null, null, null);
                }
            }
            else
            {
                // A pre-SOAP exception can race with another worker's claim.
                // Never let an unconditional unit-of-work save overwrite InFlight.
                var preflightDone = await _claimStore.TryFinishPreflightAsync(attempt.Id, response.Id,
                    new AxonNotificationCompletion(attempt.EstadoNotificacion, response.EstadoProcesamiento,
                        true, "AXON_PRETRANSPORT_TECHNICAL", "Fallo antes del transporte.", ex.GetType().Name,
                        null, null), DateTime.UtcNow, CancellationToken.None);
                if (!preflightDone)
                    return new(true, true, false, false, true, AchResponseNotificationStatus.RequiereRevisionManual,
                        AchResponseProcessingStatus.RequiereRevisionManual, "AXON_PREFLIGHT_FENCED", null, null, null);
            }
            return new(true, true, false, false, true, attempt.EstadoNotificacion, response.EstadoProcesamiento, null, null, ex.GetType().Name, null);
        }
    }

    private async Task<(string Code, string Message)?> ValidateDifferentialResponseReadinessAsync(string? reference, CancellationToken cancellationToken)
    {
        if (_operationResolver is null || _mappingReadinessService is null)
        {
            return null;
        }

        var operation = _operationResolver.ResolveDifferentialResponse(reference);
        if (operation.MovesMoney)
        {
            return ("INTEGRATION_OPERATION_INVALID", "RegistrarRespuestaTransaccion no puede clasificarse como operacion monetaria.");
        }

        var readiness = await _mappingReadinessService.EvaluateAsync(operation, cancellationToken);
        if (readiness.IsReady)
        {
            return null;
        }

        var detail = readiness.MissingRequiredMappings.Count > 0
            ? string.Join(", ", readiness.MissingRequiredMappings)
            : string.Join("; ", readiness.Errors);

        return (readiness.Code, $"No se puede registrar respuesta diferencial sin mappings requeridos activos. {detail}".Trim());
    }

    private async Task<(string Code, string Message)?> WriteDifferentialResponseTraceAsync(
        RegistrarRespuestaAchCommand cmd,
        string reference,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        if (_operationResolver is null || _mappingTraceWriter is null)
        {
            return null;
        }

        var operation = _operationResolver.ResolveDifferentialResponse(reference);
        if (operation.MovesMoney)
        {
            return ("DIFFERENTIAL_RESPONSE_NON_MONETARY_GUARDRAIL_FAILED", "RegistrarRespuestaTransaccion fue clasificada erroneamente como monetaria.");
        }

        var trace = await _mappingTraceWriter.WriteAsync(
            operation,
            cmd,
            transactionId: operation.TransactionId,
            reference: reference,
            correlationId: correlationId ?? string.Empty,
            dryRun: true,
            externalTransmission: false,
            ct: cancellationToken);

        if (trace.MissingRequiredFields.Count == 0)
        {
            return null;
        }

        return ("DIFFERENTIAL_RESPONSE_REQUIRED_FIELD_MISSING",
            $"No se puede registrar respuesta diferencial: faltan campos requeridos en mapping trace: {string.Join(", ", trace.MissingRequiredFields)}.");
    }
}
