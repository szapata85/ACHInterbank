using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Repositories;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Notification.Services;

public sealed class NotificarRespuestaAchUseCase : INotificarRespuestaAchUseCase
{
    private readonly IAchResponseRepository _responseRepository;
    private readonly IAchResponseNotificationAttemptRepository _attemptRepository;
    private readonly IRegistrarRespuestaAchCommandMapper _mapper;
    private readonly IRespuestaTransaccionesAchGateway _gateway;
    private readonly IUnitOfWork _unitOfWork;

    public NotificarRespuestaAchUseCase(IAchResponseRepository responseRepository, IAchResponseNotificationAttemptRepository attemptRepository, IRegistrarRespuestaAchCommandMapper mapper, IRespuestaTransaccionesAchGateway gateway, IUnitOfWork unitOfWork)
    {
        _responseRepository = responseRepository;
        _attemptRepository = attemptRepository;
        _mapper = mapper;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
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

        var cmd = _mapper.Map(response, attempt);

        try
        {
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
}
