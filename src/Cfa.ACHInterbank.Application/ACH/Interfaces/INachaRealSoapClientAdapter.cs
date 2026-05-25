using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaRealSoapClientAdapter
{
    Task<NachaSoapExecutionResult> ExecuteProcContrapartidasAsync(
        NachaSoapProcContrapartidasPayload payload,
        NachaSoapUatControlOptions options,
        CancellationToken cancellationToken = default);

    Task<NachaSoapExecutionResult> ExecuteProcTransaccionesAsync(
        NachaSoapProcTransaccionesPayload payload,
        NachaSoapUatControlOptions options,
        CancellationToken cancellationToken = default);

    Task<NachaSoapExecutionResult> ExecuteRegistrarRespuestaTransaccionAsync(
        NachaSoapRegistrarRespuestaTransaccionPayload payload,
        NachaSoapUatControlOptions options,
        CancellationToken cancellationToken = default);
}
