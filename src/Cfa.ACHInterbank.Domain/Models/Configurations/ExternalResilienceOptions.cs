namespace Cfa.ACHInterbank.Domain.Models.Configurations;

/// <summary>
/// Operation-specific transport resilience settings for outbound HTTP integrations.
/// Retry remains disabled by default and must be enabled only for operations proven safe to repeat.
/// </summary>
public sealed class ExternalHttpResilienceOptions
{
    public const string SectionName = "Resilience:ExternalHttp";

    public ExternalHttpPolicyOptions HealthProbe { get; set; } = new()
    {
        TimeoutSeconds = 5,
        MaxRetryAttempts = 2,
        RetryBaseDelayMilliseconds = 200,
        CircuitBreakerMinimumThroughput = 5,
        BulkheadPermitLimit = 20,
        BulkheadQueueLimit = 20
    };

    public ExternalHttpPolicyOptions Generic { get; set; } = new()
    {
        TimeoutSeconds = 100,
        MaxRetryAttempts = 0,
        CircuitBreakerMinimumThroughput = 10,
        BulkheadPermitLimit = 20,
        BulkheadQueueLimit = 50
    };

    public ExternalHttpPolicyOptions Soap { get; set; } = new()
    {
        // SOAP timeout is owned by the persisted integration mapping for each operation.
        TimeoutSeconds = 0,
        MaxRetryAttempts = 0,
        CircuitBreakerMinimumThroughput = 10,
        BulkheadPermitLimit = 10,
        BulkheadQueueLimit = 50
    };
}

public sealed class ExternalHttpPolicyOptions
{
    public int TimeoutSeconds { get; set; }
    public int MaxRetryAttempts { get; set; }
    public int RetryBaseDelayMilliseconds { get; set; } = 200;
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;
    public int CircuitBreakerSamplingDurationSeconds { get; set; } = 30;
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;
    public int CircuitBreakerBreakDurationSeconds { get; set; } = 30;
    public int BulkheadPermitLimit { get; set; } = 10;
    public int BulkheadQueueLimit { get; set; } = 50;
}
