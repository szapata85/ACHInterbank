namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class NachaSoapExecutionRequest
{
    public required NachaIncomingDecision Decision { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public string ClearingHouseCode { get; init; } = string.Empty;
    public string ProfileCode { get; init; } = string.Empty;
    public string RequestedBy { get; init; } = "system";
    public bool IsEnabled { get; init; }
    public bool DryRun { get; init; } = true;
    public NachaSoapExecutionContext? PayloadContext { get; init; }
    public NachaSoapSimulationScenario? SimulationScenario { get; init; }
    public NachaSoapSimulationOptions? SimulationOptions { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public sealed class NachaSoapMappedRequest
{
    public string Phase { get; init; } = "6B.5";
    public NachaSoapOperationCandidate Operation { get; init; } = NachaSoapOperationCandidate.None;
    public NachaIncomingDecisionType DecisionType { get; init; } = NachaIncomingDecisionType.ManualReviewRequired;
    public bool IsExecutable { get; init; }
    public bool RequiresMonetaryMovement { get; init; }
    public bool WouldInvokeSoap { get; init; }
    public bool ProductiveExecution { get; init; }
    public string MethodName { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public NachaSoapPayloadMappingResult? PayloadMapping { get; init; }
    public IReadOnlyDictionary<string, string> Payload { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyDictionary<string, string> Trace { get; init; } = new Dictionary<string, string>();
}

public sealed class NachaSoapExecutionResult
{
    public string Phase { get; init; } = "6B.5";
    public NachaSoapExecutionStatus Status { get; init; } = NachaSoapExecutionStatus.Skipped;
    public NachaSoapMappedRequest? MappedRequest { get; init; }
    public bool SoapWasInvoked { get; init; }
    public bool WasExecuted { get; init; }
    public bool SimulatedExecution { get; init; }
    public bool ProductiveExecution { get; init; }
    public string Message { get; init; } = string.Empty;
    public string ExternalReference { get; init; } = string.Empty;
    public string ResponseCode { get; init; } = string.Empty;
    public string ResponseMessage { get; init; } = string.Empty;
    public bool IsSoapFault { get; init; }
    public string SoapFaultCode { get; init; } = string.Empty;
    public string SoapFaultMessage { get; init; } = string.Empty;
    public bool IsTimeout { get; init; }
    public IReadOnlyDictionary<string, string> RequestSummary { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> ResponseSummary { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyDictionary<string, string> Trace { get; init; } = new Dictionary<string, string>();
}

public enum NachaSoapExecutionStatus
{
    Skipped = 0,
    Rejected = 1,
    DryRunCompleted = 2,
    BlockedByNoGo = 3,
    SimulatedSuccess = 4,
    SimulatedSoapFault = 5,
    SimulatedTimeout = 6,
    SimulatedFailure = 7
}
