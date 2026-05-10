using Cfa.ACHInterbank.Application.ACH.Responses.Models;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Interfaces;

public interface IRegistrarRespuestaAchUseCase
{
    Task<ResultadoRegistroRespuestaAch> ExecuteAsync(
        RegistrarRespuestaAchCommand command,
        CancellationToken cancellationToken = default);
}
