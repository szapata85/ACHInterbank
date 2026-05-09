using Cfa.ACHInterbank.Application.ACH.Responses.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Application.External.Connections;

namespace Cfa.ACHInterbank.External.Connections.RespuestaTransaccionesAch;

[Scoped]
public sealed class RespuestaTransaccionesAchGateway : IRespuestaTransaccionesAchGateway
{
    private readonly IWsAxonRespuestaTransaccionesSoapClient _soapClient;
    private readonly RegistrarRespuestaAchSoapRequestMapper _requestMapper;
    private readonly RegistrarRespuestaAchSoapResponseParser _responseParser;

    public RespuestaTransaccionesAchGateway(
        IWsAxonRespuestaTransaccionesSoapClient soapClient,
        RegistrarRespuestaAchSoapRequestMapper requestMapper,
        RegistrarRespuestaAchSoapResponseParser responseParser)
    {
        _soapClient = soapClient;
        _requestMapper = requestMapper;
        _responseParser = responseParser;
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
