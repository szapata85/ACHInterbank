namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class NachaSoapSimulationScenario
{
    public string ScenarioId { get; init; } = "default";
    public NachaSoapOperationCandidate OperationCandidate { get; init; } = NachaSoapOperationCandidate.None;
    public bool ShouldSucceed { get; init; } = true;
    public bool ShouldTimeout { get; init; }
    public bool ShouldReturnSoapFault { get; init; }
    public string SoapFaultCode { get; init; } = string.Empty;
    public string SoapFaultMessage { get; init; } = string.Empty;
    public int SimulatedLatencyMs { get; init; }
    public string SimulatedExternalReference { get; init; } = string.Empty;
    public string SimulatedResponseCode { get; init; } = "00";
    public string SimulatedResponseMessage { get; init; } = "SIMULATED_OK";
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public sealed class NachaSoapSimulationOptions
{
    public bool Enabled { get; init; } = true;
    public int DefaultLatencyMs { get; init; }
    public int MaxAllowedLatencyMs { get; init; } = 5_000;
    public bool AllowFaultSimulation { get; init; } = true;
    public bool AllowTimeoutSimulation { get; init; } = true;
    public bool ProductiveExecution { get; init; }
    public bool AllowExternalSoapInvocation { get; init; }
    public string EnvironmentName { get; init; } = "Simulation";
}

public sealed class NachaSoapAdapterExecutionResult
{
    public string AdapterName { get; init; } = string.Empty;
    public NachaSoapOperationCandidate OperationCandidate { get; init; } = NachaSoapOperationCandidate.None;
    public bool IsSuccess { get; init; }
    public bool IsTimeout { get; init; }
    public bool IsSoapFault { get; init; }
    public string SoapFaultCode { get; init; } = string.Empty;
    public string SoapFaultMessage { get; init; } = string.Empty;
    public string ExternalReference { get; init; } = string.Empty;
    public string ResponseCode { get; init; } = string.Empty;
    public string ResponseMessage { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> RequestSummary { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> ResponseSummary { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
