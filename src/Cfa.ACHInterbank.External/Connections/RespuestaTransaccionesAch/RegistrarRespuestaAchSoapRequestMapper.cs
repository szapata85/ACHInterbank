using Cfa.ACHInterbank.Application.ACH.Responses.Models;

namespace Cfa.ACHInterbank.External.Connections.RespuestaTransaccionesAch;

internal sealed class RegistrarRespuestaAchSoapRequestMapper
{
    public IReadOnlyDictionary<string, object?> Map(RegistrarRespuestaAchCommand command)
    {
        return new Dictionary<string, object?>
        {
            ["idCanal"] = command.IdCanal,
            ["nombreCanal"] = command.NombreCanal,
            ["idTransaccion"] = command.IdTransaccion,
            ["idEstado"] = command.IdEstado,
            ["causal"] = command.Causal,
            ["idTransaccionAxon"] = command.IdTransaccionServicioExterno,
            ["descripcionCausal"] = command.DescripcionCausal
        };
    }
}
