using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Models;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Processing.Interfaces;

public interface IProcesarRespuestaAchUseCase
{
    Task<ProcesarRespuestaAchResult> ExecuteAsync(ProcesarRespuestaAchCommand command, CancellationToken cancellationToken = default);
}
