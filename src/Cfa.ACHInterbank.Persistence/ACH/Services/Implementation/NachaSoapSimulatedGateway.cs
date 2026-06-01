using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaSoapSimulatedGateway : INachaSoapSimulatedGateway
{
    private readonly IReadOnlyList<INachaSoapOperationAdapter> _adapters;
    private readonly INachaSoapRequestMapper _requestMapper;

    public NachaSoapSimulatedGateway(
        IEnumerable<INachaSoapOperationAdapter> adapters,
        INachaSoapRequestMapper requestMapper)
    {
        _adapters = adapters.ToList();
        _requestMapper = requestMapper;
    }

    public async Task<NachaSoapExecutionResult> ExecuteAsync(
        NachaSoapExecutionRequest request,
        NachaSoapExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var options = request.SimulationOptions ?? new NachaSoapSimulationOptions();
        var mapped = _requestMapper.Map(request);
        var operation = request.Decision.SoapOperation;

        if (options.ProductiveExecution)
        {
            return Blocked(mapped, "ProductiveExecution=true esta bloqueado. Productivo permanece NO-GO.");
        }

        if (options.AllowExternalSoapInvocation)
        {
            return Blocked(mapped, "AllowExternalSoapInvocation=true esta bloqueado en gateway simulado.");
        }

        if (!options.Enabled)
        {
            return Skipped(mapped, "Gateway SOAP simulado deshabilitado.");
        }

        if (NachaSoapMockOperationAdapter.ContainsSensitiveMetadata(request.Metadata)
            || NachaSoapMockOperationAdapter.ContainsSensitiveMetadata(context.Metadata)
            || NachaSoapMockOperationAdapter.ContainsSensitiveMetadata(request.SimulationScenario?.Metadata ?? new Dictionary<string, string>()))
        {
            return Rejected(mapped, "Metadata contiene claves sensibles no permitidas.");
        }

        if (operation == NachaSoapOperationCandidate.None)
        {
            return Skipped(mapped, "Operacion None omitida por gateway simulado.");
        }

        if (request.Decision.DecisionType is NachaIncomingDecisionType.ManualReviewRequired or NachaIncomingDecisionType.IgnoreDuplicate)
        {
            return Skipped(mapped, $"{request.Decision.DecisionType} omitido por gateway simulado.");
        }

        if (!mapped.IsExecutable)
        {
            return Rejected(mapped, "Request SOAP no ejecutable por validacion interna.");
        }

        var adapter = _adapters.FirstOrDefault(x => x.CanHandle(operation));
        if (adapter is null)
        {
            return Rejected(mapped, $"No existe adapter simulado para {operation}.");
        }

        if (request.SimulationScenario?.ShouldTimeout == true && !options.AllowTimeoutSimulation)
        {
            return Rejected(mapped, "Timeout simulation no permitida por opciones.");
        }

        if (request.SimulationScenario?.ShouldReturnSoapFault == true && !options.AllowFaultSimulation)
        {
            return Rejected(mapped, "SOAP fault simulation no permitida por opciones.");
        }

        try
        {
            var adapterResult = await adapter.ExecuteAsync(request, context, cancellationToken);
            return FromAdapter(mapped, adapterResult);
        }
        catch (Exception ex)
        {
            return Rejected(mapped, $"Fallo controlado en adapter simulado: {ex.Message}");
        }
    }

    private static NachaSoapExecutionResult FromAdapter(
        NachaSoapMappedRequest mapped,
        NachaSoapAdapterExecutionResult adapterResult)
    {
        var status = adapterResult.IsTimeout
            ? NachaSoapExecutionStatus.SimulatedTimeout
            : adapterResult.IsSoapFault
                ? NachaSoapExecutionStatus.SimulatedSoapFault
                : adapterResult.IsSuccess
                    ? NachaSoapExecutionStatus.SimulatedSuccess
                    : NachaSoapExecutionStatus.SimulatedFailure;

        return new NachaSoapExecutionResult
        {
            Status = status,
            MappedRequest = mapped,
            SoapWasInvoked = false,
            WasExecuted = false,
            SimulatedExecution = true,
            ProductiveExecution = false,
            Message = adapterResult.ResponseMessage,
            ExternalReference = adapterResult.ExternalReference,
            ResponseCode = adapterResult.ResponseCode,
            ResponseMessage = adapterResult.ResponseMessage,
            IsSoapFault = adapterResult.IsSoapFault,
            SoapFaultCode = adapterResult.SoapFaultCode,
            SoapFaultMessage = adapterResult.SoapFaultMessage,
            IsTimeout = adapterResult.IsTimeout,
            RequestSummary = adapterResult.RequestSummary,
            ResponseSummary = adapterResult.ResponseSummary,
            Trace = BuildTrace(mapped.Operation, status)
        };
    }

    private static NachaSoapExecutionResult Blocked(NachaSoapMappedRequest mapped, string message)
        => BuildSimple(mapped, NachaSoapExecutionStatus.BlockedByNoGo, message);

    private static NachaSoapExecutionResult Rejected(NachaSoapMappedRequest mapped, string message)
        => BuildSimple(mapped, NachaSoapExecutionStatus.Rejected, message);

    private static NachaSoapExecutionResult Skipped(NachaSoapMappedRequest mapped, string message)
        => BuildSimple(mapped, NachaSoapExecutionStatus.Skipped, message);

    private static NachaSoapExecutionResult BuildSimple(
        NachaSoapMappedRequest mapped,
        NachaSoapExecutionStatus status,
        string message)
        => new()
        {
            Status = status,
            MappedRequest = mapped,
            SoapWasInvoked = false,
            WasExecuted = false,
            SimulatedExecution = true,
            ProductiveExecution = false,
            Message = message,
            ResponseCode = status.ToString(),
            ResponseMessage = message,
            Errors = status is NachaSoapExecutionStatus.Rejected or NachaSoapExecutionStatus.BlockedByNoGo ? [message] : [],
            RequestSummary = mapped.PayloadMapping?.SanitizedSummary ?? new Dictionary<string, string>(),
            ResponseSummary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Phase"] = "6B.5",
                ["OperationCandidate"] = mapped.Operation.ToString(),
                ["ResponseCode"] = status.ToString(),
                ["ResponseMessage"] = message,
                ["SimulatedExecution"] = "true",
                ["ProductiveExecution"] = "false"
            },
            Trace = BuildTrace(mapped.Operation, status)
        };

    private static Dictionary<string, string> BuildTrace(
        NachaSoapOperationCandidate operation,
        NachaSoapExecutionStatus status)
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["Phase"] = "6B.5",
            ["Operation"] = operation.ToString(),
            ["Status"] = status.ToString(),
            ["SoapWasInvoked"] = "false",
            ["WasExecuted"] = "false",
            ["SimulatedExecution"] = "true",
            ["ProductiveExecution"] = "false",
            ["NoGoReason"] = "Productivo permanece NO-GO; gateway SOAP simulado no invoca servicios externos."
        };
}
