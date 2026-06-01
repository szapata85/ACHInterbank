using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaSoapMockOperationAdapter : INachaSoapOperationAdapter
{
    public string AdapterName => nameof(NachaSoapMockOperationAdapter);

    public bool CanHandle(NachaSoapOperationCandidate operationCandidate)
        => operationCandidate is NachaSoapOperationCandidate.ProcContrapartidas
            or NachaSoapOperationCandidate.ProcTransacciones
            or NachaSoapOperationCandidate.RegistrarRespuestaTransaccion;

    public Task<NachaSoapAdapterExecutionResult> ExecuteAsync(
        NachaSoapExecutionRequest request,
        NachaSoapExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var operation = request.Decision.SoapOperation;
        var scenario = request.SimulationScenario ?? new NachaSoapSimulationScenario { OperationCandidate = operation };
        var payload = request.PayloadContext is null
            ? null
            : new NachaSoapPayloadMapper().Map(request.Decision, request.PayloadContext);

        if (operation is NachaSoapOperationCandidate.ProcContrapartidas or NachaSoapOperationCandidate.ProcTransacciones
            && !request.Decision.RequiresMonetaryMovement)
        {
            return Task.FromResult(Failure(operation, "MONETARY_REQUIRED", $"{operation} requiere movimiento monetario.", payload));
        }

        if (operation == NachaSoapOperationCandidate.RegistrarRespuestaTransaccion && request.Decision.RequiresMonetaryMovement)
        {
            return Task.FromResult(Failure(operation, "NON_MONETARY_REQUIRED", "RegistrarRespuestaTransaccion no permite movimiento monetario.", payload));
        }

        if (scenario.ShouldTimeout)
        {
            return Task.FromResult(new NachaSoapAdapterExecutionResult
            {
                AdapterName = AdapterName,
                OperationCandidate = operation,
                IsTimeout = true,
                ResponseCode = "TIMEOUT",
                ResponseMessage = "Timeout SOAP simulado.",
                RequestSummary = BuildRequestSummary(request, payload),
                ResponseSummary = BuildResponseSummary(operation, "TIMEOUT", "Timeout SOAP simulado.", scenario.SimulatedExternalReference),
                Metadata = SanitizeMetadata(scenario.Metadata)
            });
        }

        if (scenario.ShouldReturnSoapFault)
        {
            var code = string.IsNullOrWhiteSpace(scenario.SoapFaultCode) ? "SOAP_FAULT" : scenario.SoapFaultCode;
            var message = string.IsNullOrWhiteSpace(scenario.SoapFaultMessage) ? "SOAP fault simulado." : scenario.SoapFaultMessage;
            return Task.FromResult(new NachaSoapAdapterExecutionResult
            {
                AdapterName = AdapterName,
                OperationCandidate = operation,
                IsSoapFault = true,
                SoapFaultCode = code,
                SoapFaultMessage = message,
                ResponseCode = code,
                ResponseMessage = message,
                RequestSummary = BuildRequestSummary(request, payload),
                ResponseSummary = BuildResponseSummary(operation, code, message, scenario.SimulatedExternalReference),
                Metadata = SanitizeMetadata(scenario.Metadata)
            });
        }

        return Task.FromResult(new NachaSoapAdapterExecutionResult
        {
            AdapterName = AdapterName,
            OperationCandidate = operation,
            IsSuccess = scenario.ShouldSucceed,
            ExternalReference = string.IsNullOrWhiteSpace(scenario.SimulatedExternalReference)
                ? $"SIM-{operation}-{request.CorrelationId}"
                : scenario.SimulatedExternalReference,
            ResponseCode = string.IsNullOrWhiteSpace(scenario.SimulatedResponseCode) ? "00" : scenario.SimulatedResponseCode,
            ResponseMessage = string.IsNullOrWhiteSpace(scenario.SimulatedResponseMessage) ? "SIMULATED_OK" : scenario.SimulatedResponseMessage,
            RequestSummary = BuildRequestSummary(request, payload),
            ResponseSummary = BuildResponseSummary(
                operation,
                string.IsNullOrWhiteSpace(scenario.SimulatedResponseCode) ? "00" : scenario.SimulatedResponseCode,
                string.IsNullOrWhiteSpace(scenario.SimulatedResponseMessage) ? "SIMULATED_OK" : scenario.SimulatedResponseMessage,
                string.IsNullOrWhiteSpace(scenario.SimulatedExternalReference) ? $"SIM-{operation}-{request.CorrelationId}" : scenario.SimulatedExternalReference),
            Metadata = SanitizeMetadata(scenario.Metadata)
        });
    }

    private NachaSoapAdapterExecutionResult Failure(
        NachaSoapOperationCandidate operation,
        string code,
        string message,
        NachaSoapPayloadMappingResult? payload)
        => new()
        {
            AdapterName = AdapterName,
            OperationCandidate = operation,
            IsSuccess = false,
            ResponseCode = code,
            ResponseMessage = message,
            RequestSummary = payload?.SanitizedSummary ?? new Dictionary<string, string>(),
            ResponseSummary = BuildResponseSummary(operation, code, message, string.Empty)
        };

    private static Dictionary<string, string> BuildRequestSummary(
        NachaSoapExecutionRequest request,
        NachaSoapPayloadMappingResult? payload)
    {
        var summary = payload?.SanitizedSummary.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase)
                      ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        summary["Phase"] = "6B.5";
        summary["OperationCandidate"] = request.Decision.SoapOperation.ToString();
        summary["CorrelationId"] = request.CorrelationId;
        summary["SimulatedExecution"] = "true";
        summary["ProductiveExecution"] = "false";
        return summary;
    }

    private static Dictionary<string, string> BuildResponseSummary(
        NachaSoapOperationCandidate operation,
        string responseCode,
        string responseMessage,
        string externalReference)
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["Phase"] = "6B.5",
            ["OperationCandidate"] = operation.ToString(),
            ["ResponseCode"] = responseCode,
            ["ResponseMessage"] = responseMessage,
            ["ExternalReference"] = externalReference,
            ["SimulatedExecution"] = "true",
            ["ProductiveExecution"] = "false"
        };

    internal static Dictionary<string, string> SanitizeMetadata(IReadOnlyDictionary<string, string> metadata)
        => metadata
            .Where(x => !IsSensitiveKey(x.Key))
            .ToDictionary(x => x.Key, x => MaskValue(x.Value), StringComparer.OrdinalIgnoreCase);

    internal static bool ContainsSensitiveMetadata(IReadOnlyDictionary<string, string> metadata)
        => metadata.Any(x => IsSensitiveKey(x.Key));

    private static bool IsSensitiveKey(string key)
        => key.Contains("password", StringComparison.OrdinalIgnoreCase)
           || key.Contains("token", StringComparison.OrdinalIgnoreCase)
           || key.Contains("secret", StringComparison.OrdinalIgnoreCase)
           || key.Contains("credential", StringComparison.OrdinalIgnoreCase);

    private static string MaskValue(string value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length >= 7 ? $"***{digits[^4..]}" : value ?? string.Empty;
    }
}
