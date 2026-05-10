using Cfa.ACHInterbank.Application.ACH.Responses.Models;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Interfaces;

public interface IRespuestaTransaccionesAchGateway
{
    Task<ResultadoRegistroRespuestaAch> RegistrarRespuestaAsync(
        RegistrarRespuestaAchCommand command,
        CancellationToken cancellationToken = default);
}
