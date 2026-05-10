using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Models;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Notification.Interfaces;

public interface INotificarRespuestaAchUseCase
{
    Task<NotificarRespuestaAchResult> ExecuteAsync(NotificarRespuestaAchCommand command, CancellationToken cancellationToken = default);
}
