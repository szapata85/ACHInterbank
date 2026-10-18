using Cfa.ACHInterbank.Application.ACH.Responses.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.External.Connections.RespuestaTransaccionesAch;

public sealed class RespuestaTransaccionesAchGateway : IRespuestaTransaccionesAchGateway
{
    private readonly IWsAxonRespuestaTransaccionesSoapClient _soapClient;
    private readonly RegistrarRespuestaAchSoapRequestMapper _requestMapper = new();
    private readonly RegistrarRespuestaAchSoapResponseParser _responseParser = new();
    private readonly RegistrarRespuestaRequestParityPolicy _parityPolicy = new();
    private readonly RegistrarRespuestaDispatchOptions _dispatchOptions;
    private readonly IIntegrationRequestResolver? _requestResolver;
    private readonly ISoapRequestBodyWriter? _soapRequestBodyWriter;
    private readonly TimeProvider _timeProvider;

    public RespuestaTransaccionesAchGateway(
        IWsAxonRespuestaTransaccionesSoapClient soapClient,
        IOptions<RegistrarRespuestaDispatchOptions>? dispatchOptions = null,
        IIntegrationRequestResolver? requestResolver = null,
        ISoapRequestBodyWriter? soapRequestBodyWriter = null,
        TimeProvider? timeProvider = null)
    {
        _soapClient = soapClient ?? throw new ArgumentNullException(nameof(soapClient));
        _dispatchOptions = dispatchOptions?.Value ?? new RegistrarRespuestaDispatchOptions();
        _requestResolver = requestResolver;
        _soapRequestBodyWriter = soapRequestBodyWriter;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ResultadoRegistroRespuestaAch> RegistrarRespuestaAsync(
        RegistrarRespuestaAchCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        string soapResponse;
        if (_dispatchOptions.UseContractDrivenRequest)
        {
            var operationXml = await BuildContractDrivenOperationAsync(command, cancellationToken).ConfigureAwait(false);
            soapResponse = await _soapClient
                .RegistrarRespuestaTransaccionAsync(operationXml, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            var physicalRequest = _requestMapper.Map(command);
            soapResponse = await _soapClient
                .RegistrarRespuestaTransaccionAsync(physicalRequest, cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            return _responseParser.Parse(soapResponse);
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
            throw new InvalidOperationException("Error parsing SOAP response for ACH response registration.", ex);
        }
    }

    private async Task<string> BuildContractDrivenOperationAsync(
        RegistrarRespuestaAchCommand command,
        CancellationToken cancellationToken)
    {
        if (_requestResolver is null || _soapRequestBodyWriter is null)
        {
            throw new InvalidOperationException(
                "REGISTRAR_RESPUESTA_CONTRACT_ENGINE_NOT_CONFIGURED: el resolver y writer contract-driven son obligatorios cuando el feature flag está activo.");
        }

        if (!command.DifferentialResponseId.HasValue || command.DifferentialResponseId.Value == Guid.Empty)
        {
            throw new InvalidOperationException(
                "REGISTRAR_RESPUESTA_DIFFERENTIAL_RESPONSE_REQUIRED: se requiere la identidad de la respuesta diferencial para resolver las fuentes contract-driven.");
        }

        var correlationId = string.IsNullOrWhiteSpace(command.CorrelationId)
            ? $"registrar-respuesta:{command.DifferentialResponseId.Value:D}"
            : command.CorrelationId.Trim();

        var context = new IntegrationExecutionContext(
            RegistrarRespuestaRequestParityPolicy.MethodCode,
            correlationId,
            _timeProvider.GetUtcNow().UtcDateTime,
            differentialResponseId: command.DifferentialResponseId.Value);

        var resolved = await _requestResolver.ResolveAsync(context, cancellationToken).ConfigureAwait(false);
        var baselineRequest = _parityPolicy.EnsureBaselineV1(resolved, command);
        var envelopeXml = _soapRequestBodyWriter.Write(baselineRequest);
        return RegistrarRespuestaSoapOperationAdapter.ExtractOperation(envelopeXml);
    }
}
