namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class NachaSoapRetryPolicy
{
    public int MaxAttempts { get; init; } = 1;
    public int BaseDelayMs { get; init; }
    public int MaxDelayMs { get; init; }
    public bool UseExponentialBackoff { get; init; } = true;
    public bool RetryOnTimeout { get; init; }
    public bool RetryOnSoapFault { get; init; }
    public bool RetryOnTransientFailure { get; init; }
    public IReadOnlySet<string> NonRetryableErrorCodes { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public sealed class NachaSoapAttemptAudit
{
    public string AttemptId { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public string IdempotencyKey { get; init; } = string.Empty;
    public NachaSoapOperationCandidate OperationCandidate { get; init; } = NachaSoapOperationCandidate.None;
    public int AttemptNumber { get; init; }
    public int MaxAttempts { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime FinishedAt { get; init; }
    public long DurationMs { get; init; }
    public bool IsSuccess { get; init; }
    public bool IsTimeout { get; init; }
    public bool IsSoapFault { get; init; }
    public bool IsTransient { get; init; }
    public bool IsRetryable { get; init; }
    public NachaSoapFailureClassification Classification { get; init; } = NachaSoapFailureClassification.None;
    public string ErrorCode { get; init; } = string.Empty;
    public string ErrorDescription { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> SanitizedRequestSummary { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> SanitizedResponseSummary { get; init; } = new Dictionary<string, string>();
    public string Phase { get; init; } = "6B.5";
    public bool ProductiveExecution { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public sealed class NachaSoapIdempotencyRecord
{
    public string IdempotencyKey { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public NachaSoapOperationCandidate OperationCandidate { get; init; } = NachaSoapOperationCandidate.None;
    public int? TransactionId { get; init; }
    public int? PrenotificationId { get; init; }
    public string EntryTraceNumber { get; init; } = string.Empty;
    public string OriginalTraceNumber { get; init; } = string.Empty;
    public string SourceFileName { get; init; } = string.Empty;
    public string RequestHash { get; init; } = string.Empty;
    public NachaSoapIdempotencyStatus Status { get; set; } = NachaSoapIdempotencyStatus.New;
    public DateTime FirstSeenAt { get; init; }
    public DateTime LastAttemptAt { get; set; }
    public int AttemptCount { get; set; }
    public string FinalResult { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public sealed class NachaSoapIdempotencyBeginResult
{
    public bool CanExecute { get; init; }
    public required NachaSoapIdempotencyRecord Record { get; init; }
}

public sealed class NachaSoapResilienceExecutionResult
{
    public string CorrelationId { get; init; } = string.Empty;
    public string IdempotencyKey { get; init; } = string.Empty;
    public NachaSoapOperationCandidate OperationCandidate { get; init; } = NachaSoapOperationCandidate.None;
    public bool IsSuccess { get; init; }
    public bool WasSkipped { get; init; }
    public bool WasExecuted { get; init; }
    public bool WasRetried { get; init; }
    public int AttemptCount { get; init; }
    public NachaSoapIdempotencyStatus FinalStatus { get; init; } = NachaSoapIdempotencyStatus.New;
    public string FinalMessage { get; init; } = string.Empty;
    public IReadOnlyList<NachaSoapAttemptAudit> Attempts { get; init; } = [];
    public NachaSoapExecutionResult? LastExecutionResult { get; init; }
    public string Phase { get; init; } = "6B.5";
    public bool ProductiveExecution { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public enum NachaSoapIdempotencyStatus
{
    New = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    SkippedDuplicate = 4,
    BlockedByNoGo = 5,
    ManualReviewRequired = 6
}

public enum NachaSoapFailureClassification
{
    None = 0,
    Timeout = 1,
    SoapFault = 2,
    TransientFailure = 3,
    NonRetryableFailure = 4,
    ValidationFailure = 5,
    SecurityBlocked = 6,
    DuplicateBlocked = 7
}
