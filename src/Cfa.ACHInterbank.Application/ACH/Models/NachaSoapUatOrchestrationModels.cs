namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class NachaSoapUatReadinessRequest
{
    public string CorrelationId { get; init; } = string.Empty;
    public string SourceFileName { get; init; } = string.Empty;
    public string ClearingHouseCode { get; init; } = string.Empty;
    public string ProfileCode { get; init; } = string.Empty;
    public required NachaIncomingDecision Decision { get; init; }
    public required NachaSoapExecutionContext ExecutionContext { get; init; }
    public required NachaSoapUatControlOptions UatControlOptions { get; init; }
    public NachaSoapSimulationOptions SimulationOptions { get; init; } = new();
    public NachaSoapSimulationScenario? SimulationScenario { get; init; }
    public IReadOnlyList<NachaSoapSimulationScenario> SimulationAttempts { get; init; } = [];
    public NachaSoapRetryPolicy RetryPolicy { get; init; } = new();
    public IReadOnlyList<NachaSoapEndpointDescriptor> EndpointDescriptors { get; init; } = [];
    public string RequestedBy { get; init; } = "system";
    public DateTime RequestedAt { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public sealed class NachaSoapUatReadinessResult
{
    public string CorrelationId { get; init; } = string.Empty;
    public NachaSoapOperationCandidate OperationCandidate { get; init; } = NachaSoapOperationCandidate.None;
    public NachaSoapUatReadinessStatus Status { get; init; } = NachaSoapUatReadinessStatus.Failed;
    public bool IsReadyForUat { get; init; }
    public bool IsBlocked { get; init; }
    public IReadOnlyList<string> BlockReasons { get; init; } = [];
    public bool PayloadMappingPassed { get; init; }
    public bool RequestMappingPassed { get; init; }
    public bool OperationalGatePassed { get; init; }
    public bool ReadinessCheckPassed { get; init; }
    public bool SimulationPassed { get; init; }
    public bool ResiliencePassed { get; init; }
    public bool WouldInvokeRealSoap { get; init; }
    public bool ProductiveExecution { get; init; }
    public bool RequiresMonetaryMovement { get; init; }
    public string Phase { get; init; } = "6B.5";
    public IReadOnlyDictionary<string, string> PayloadSummary { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> RequestSummary { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> ReadinessSummary { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> SimulationSummary { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> ResilienceSummary { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<NachaSoapUatAuditEvent> AuditTrail { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public sealed class NachaSoapUatAuditEvent
{
    public string EventId { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public NachaSoapOperationCandidate OperationCandidate { get; init; } = NachaSoapOperationCandidate.None;
    public string Message { get; init; } = string.Empty;
    public bool IsBlocked { get; init; }
    public string Severity { get; init; } = "Information";
    public DateTime Timestamp { get; init; }
    public string Phase { get; init; } = "6B.5";
    public bool ProductiveExecution { get; init; }
    public IReadOnlyDictionary<string, string> SanitizedDetails { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public enum NachaSoapUatReadinessStatus
{
    ReadyForDryRun = 0,
    ReadyForSimulation = 1,
    ReadyForUatControl = 2,
    BlockedByNoGo = 3,
    BlockedByConfiguration = 4,
    BlockedByEndpoint = 5,
    BlockedByCertificate = 6,
    BlockedByValidation = 7,
    ManualReviewRequired = 8,
    Failed = 9
}
