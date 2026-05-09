using Cfa.ACHInterbank.Application.ACH.Responses.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Application.External.Connections;

namespace Cfa.ACHInterbank.External.Connections.RespuestaTransaccionesAch;

public sealed class RespuestaTransaccionesAchGateway : IRespuestaTransaccionesAchGateway
{
    private readonly IWsAxonRespuestaTransaccionesSoapClient _soapClient;
    private readonly RegistrarRespuestaAchSoapRequestMapper _requestMapper = new();
    private readonly RegistrarRespuestaAchSoapResponseParser _responseParser = new();

    public RespuestaTransaccionesAchGateway(IWsAxonRespuestaTransaccionesSoapClient soapClient)
    {
        _soapClient = soapClient;
    }

    public async Task<ResultadoRegistroRespuestaAch> RegistrarRespuestaAsync(RegistrarRespuestaAchCommand command, CancellationToken cancellationToken = default)
    {
        var physicalRequest = _requestMapper.Map(command);
        var soapResponse = await _soapClient.RegistrarRespuestaTransaccionAsync(physicalRequest, cancellationToken).ConfigureAwait(false);

        try
        {
            return _responseParser.Parse(soapResponse);
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
            throw new InvalidOperationException("Error parsing SOAP response for ACH response registration.", ex);
        }
    }
}
