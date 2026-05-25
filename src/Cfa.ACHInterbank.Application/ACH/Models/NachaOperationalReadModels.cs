namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record NachaOperationalSummaryReadModel
{
    public required string ProductiveStatus { get; init; }
    public required string BackendPhase { get; init; }
    public required string SoapMode { get; init; }
    public bool ProductiveExecution { get; init; }
    public bool WouldInvokeRealSoap { get; init; }
    public int TotalFiles { get; init; }
    public int TotalIncomingFiles { get; init; }
    public int TotalOutgoingFiles { get; init; }
    public int TotalReturnFiles { get; init; }
    public int TotalDecisions { get; init; }
    public int TotalSoapCandidates { get; init; }
    public int TotalNoGoBlocks { get; init; }
    public int TotalManualReview { get; init; }
    public int TotalReadinessChecks { get; init; }
    public DateTimeOffset LastUpdatedAt { get; init; }
    public bool IsDemoData { get; init; }
    public bool IsPartialData { get; init; }
    public string DataSource { get; init; } = "demo-safe";
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record NachaOperationalFileReadModel
{
    public required string FileId { get; init; }
    public required string FileName { get; init; }
    public string DataSource { get; init; } = "demo-safe";
    public string? HeaderId { get; init; }
    public int PersistedRecordCount { get; init; }
    public DateTimeOffset? LastParsedAt { get; init; }
    public bool NoSensitiveData { get; init; } = true;
    public required string ClearingHouseCode { get; init; }
    public required string ProfileCode { get; init; }
    public required string FlowType { get; init; }
    public bool IsReturnFile { get; init; }
    public bool ValidationPassed { get; init; }
    public int BatchCount { get; init; }
    public int EntryCount { get; init; }
    public int AddendaCount { get; init; }
    public int BatchControlCount { get; init; }
    public int FileControlCount { get; init; }
    public required string ProcessingStatus { get; init; }
    public DateTimeOffset? ReceivedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public required string CorrelationId { get; init; }
    public bool HasErrors { get; init; }
    public int WarningCount { get; init; }
    public int ErrorCount { get; init; }
}

public sealed record NachaOperationalDecisionReadModel
{
    public required string CorrelationId { get; init; }
    public required string FileName { get; init; }
    public required string EntryTraceNumber { get; init; }
    public string? OriginalTraceNumber { get; init; }
    public required string DecisionType { get; init; }
    public required string SoapOperationCandidate { get; init; }
    public bool RequiresMonetaryMovement { get; init; }
    public required string ReasonCode { get; init; }
    public required string ReasonDescription { get; init; }
    public required string NewInternalStatus { get; init; }
    public bool ManualReviewRequired { get; init; }
    public bool IsBlocked { get; init; }
    public string? BlockReason { get; init; }
    public string DataSource { get; init; } = "demo-safe";
    public bool IsDerived { get; init; }
    public bool IsPersisted { get; init; }
    public string? Warning { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NachaSoapReadinessReadModel
{
    public required string CorrelationId { get; init; }
    public required string OperationCandidate { get; init; }
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
    public required string Phase { get; init; }
    public string DataSource { get; init; } = "demo-safe";
    public bool IsDerived { get; init; }
    public bool IsPersisted { get; init; }
    public string? Warning { get; init; }
    public DateTimeOffset LastCheckedAt { get; init; }
}

public sealed record NachaOperationalAuditReadModel
{
    public required string CorrelationId { get; init; }
    public required string Phase { get; init; }
    public required string EventType { get; init; }
    public required string Severity { get; init; }
    public required string Message { get; init; }
    public bool IsBlocked { get; init; }
    public string DataSource { get; init; } = "demo-safe";
    public bool IsDerived { get; init; }
    public bool IsPersisted { get; init; }
    public string? Warning { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public IReadOnlyDictionary<string, string> SanitizedDetails { get; init; } = new Dictionary<string, string>();
}

public sealed record NachaOperationalDashboardReadModel
{
    public required NachaOperationalSummaryReadModel Summary { get; init; }
    public IReadOnlyList<NachaOperationalFileReadModel> Files { get; init; } = [];
    public IReadOnlyList<NachaOperationalDecisionReadModel> Decisions { get; init; } = [];
    public IReadOnlyList<NachaSoapReadinessReadModel> Readiness { get; init; } = [];
    public IReadOnlyList<NachaOperationalAuditReadModel> Audit { get; init; } = [];
    public DateTimeOffset GeneratedAt { get; init; }
    public bool IsDemoData { get; init; }
    public bool IsPartialData { get; init; }
    public string DataSource { get; init; } = "demo-safe";
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public required string ProductiveStatus { get; init; }
}
