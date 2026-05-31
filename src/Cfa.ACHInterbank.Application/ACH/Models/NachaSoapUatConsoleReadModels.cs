namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record NachaSoapUatConsoleDashboardReadModel
{
    public required string ProductiveStatus { get; init; }
    public bool ProductiveExecution { get; init; }
    public bool WouldInvokeRealSoap { get; init; }
    public int TotalCandidates { get; init; }
    public int TotalReadyForUat { get; init; }
    public int TotalBlocked { get; init; }
    public int TotalManualReview { get; init; }
    public int TotalRegistrarRespuesta { get; init; }
    public int TotalProcTransacciones { get; init; }
    public int TotalProcContrapartidas { get; init; }
    public int TotalNone { get; init; }
    public int TotalSimulationPassed { get; init; }
    public int TotalSimulationFailed { get; init; }
    public int TotalResilienceWarnings { get; init; }
    public int TotalDuplicateOrIdempotent { get; init; }
    public DateTimeOffset LastUpdatedAt { get; init; }
    public required string DataSource { get; init; }
    public bool IsPartialData { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record NachaSoapUatCandidateReadModel
{
    public required string CorrelationId { get; init; }
    public string? FileId { get; init; }
    public required string FileName { get; init; }
    public required string EntryTraceNumber { get; init; }
    public required string DecisionType { get; init; }
    public required string OperationCandidate { get; init; }
    public bool RequiresMonetaryMovement { get; init; }
    public bool ProductiveExecution { get; init; }
    public bool WouldInvokeRealSoap { get; init; }
    public bool IsReadyForUat { get; init; }
    public bool IsBlocked { get; init; }
    public IReadOnlyList<string> BlockReasons { get; init; } = [];
    public bool ManualReviewRequired { get; init; }
    public required string ReadinessStatus { get; init; }
    public required string SimulationStatus { get; init; }
    public required string ResilienceStatus { get; init; }
    public required string IdempotencyStatus { get; init; }
    public DateTimeOffset? LastAttemptAt { get; init; }
    public int AttemptCount { get; init; }
    public required string DataSource { get; init; }
    public bool IsPersisted { get; init; }
    public bool IsDerived { get; init; }
    public string? Warning { get; init; }
}

public sealed record NachaSoapUatAuditReadModel
{
    public required string CorrelationId { get; init; }
    public required string Phase { get; init; }
    public required string EventType { get; init; }
    public required string Severity { get; init; }
    public required string Message { get; init; }
    public bool IsBlocked { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public IReadOnlyDictionary<string, string> SanitizedDetails { get; init; } = new Dictionary<string, string>();
    public required string DataSource { get; init; }
    public bool IsPersisted { get; init; }
}
