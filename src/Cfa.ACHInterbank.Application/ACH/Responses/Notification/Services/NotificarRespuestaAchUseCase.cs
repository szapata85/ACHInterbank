using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Repositories;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

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

    public NotificarRespuestaAchUseCase(
        IAchResponseRepository responseRepository,
        IAchResponseNotificationAttemptRepository attemptRepository,
        IRegistrarRespuestaAchCommandMapper mapper,
        IRespuestaTransaccionesAchGateway gateway,
        IUnitOfWork unitOfWork,
        ITransactionIntegrationOperationResolver? operationResolver = null,
        IIntegrationMappingReadinessService? mappingReadinessService = null,
        IIntegrationMappingTraceWriter? mappingTraceWriter = null)
    {
        _responseRepository = responseRepository;
        _attemptRepository = attemptRepository;
        _mapper = mapper;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _operationResolver = operationResolver;
        _mappingReadinessService = mappingReadinessService;
        _mappingTraceWriter = mappingTraceWriter;
    }

    public async Task<NotificarRespuestaAchResult> ExecuteAsync(NotificarRespuestaAchCommand command, CancellationToken cancellationToken = default)
    {
        if (command.NotificationAttemptId <= 0)
            return new(false, false, false, false, false, null, null, null, null, null, "NotificationAttemptId inválido");

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
            await _attemptRepository.UpdateAsync(attempt, cancellationToken);
            await _responseRepository.UpdateAsync(response, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return new(true, true, false, true, false, attempt.EstadoNotificacion, response.EstadoProcesamiento, readinessError.Value.Code, readinessError.Value.Message, null, null);
        }

        var cmd = _mapper.Map(response, attempt);

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
                await _attemptRepository.UpdateAsync(attempt, cancellationToken);
                await _responseRepository.UpdateAsync(response, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return new(true, true, false, true, false, attempt.EstadoNotificacion, response.EstadoProcesamiento, traceError.Value.Code, traceError.Value.Message, null, null);
            }

            var result = await _gateway.RegistrarRespuestaAsync(cmd, cancellationToken);
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
            await _attemptRepository.UpdateAsync(attempt, cancellationToken);
            await _responseRepository.UpdateAsync(response, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return new(true, true, false, result.ExisteError, false, attempt.EstadoNotificacion, response.EstadoProcesamiento, result.CodigoError, result.DescripcionError, null, null);
        }
        catch (Exception ex)
        {
            attempt.EstadoNotificacion = AchResponseNotificationStatus.ErrorTecnico;
            attempt.ErrorTecnico = ex.Message;
            attempt.FechaEnvio = DateTime.UtcNow;
            response.EstadoProcesamiento = AchResponseProcessingStatus.PendienteReintento;
            response.FechaActualizacion = DateTime.UtcNow;
            await _attemptRepository.UpdateAsync(attempt, cancellationToken);
            await _responseRepository.UpdateAsync(response, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return new(true, true, false, false, true, attempt.EstadoNotificacion, response.EstadoProcesamiento, null, null, ex.Message, null);
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
