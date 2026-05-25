using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaBlockedRealSoapClientAdapter : INachaRealSoapClientAdapter
{
    public Task<NachaSoapExecutionResult> ExecuteProcContrapartidasAsync(
        NachaSoapProcContrapartidasPayload payload,
        NachaSoapUatControlOptions options,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Blocked(NachaSoapOperationCandidate.ProcContrapartidas, payload.CorrelationId, options));

    public Task<NachaSoapExecutionResult> ExecuteProcTransaccionesAsync(
        NachaSoapProcTransaccionesPayload payload,
        NachaSoapUatControlOptions options,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Blocked(NachaSoapOperationCandidate.ProcTransacciones, payload.CorrelationId, options));

    public Task<NachaSoapExecutionResult> ExecuteRegistrarRespuestaTransaccionAsync(
        NachaSoapRegistrarRespuestaTransaccionPayload payload,
        NachaSoapUatControlOptions options,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Blocked(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, payload.CorrelationId, options));

    private static NachaSoapExecutionResult Blocked(
        NachaSoapOperationCandidate operation,
        string correlationId,
        NachaSoapUatControlOptions options)
        => new()
        {
            Status = NachaSoapExecutionStatus.BlockedByNoGo,
            SoapWasInvoked = false,
            WasExecuted = false,
            SimulatedExecution = false,
            ProductiveExecution = false,
            Message = "Real SOAP invocation is blocked because Productivo remains NO-GO.",
            ResponseCode = "BLOCKED_BY_NO_GO",
            ResponseMessage = "Real SOAP invocation is blocked because Productivo remains NO-GO.",
            RequestSummary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Phase"] = "6B.5",
                ["OperationCandidate"] = operation.ToString(),
                ["CorrelationId"] = correlationId,
                ["EnvironmentName"] = options.EnvironmentName,
                ["WouldInvokeRealSoap"] = "false",
                ["ProductiveExecution"] = "false"
            },
            ResponseSummary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Phase"] = "6B.5",
                ["Status"] = "BlockedByNoGo",
                ["Productivo"] = "NO-GO"
            },
            Errors = ["Real SOAP invocation is blocked because Productivo remains NO-GO."],
            Trace = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Phase"] = "6B.5",
                ["Operation"] = operation.ToString(),
                ["SoapWasInvoked"] = "false",
                ["WasExecuted"] = "false",
                ["NoGoReason"] = "Productivo permanece NO-GO; cliente SOAP real bloqueado."
            }
        };
}
